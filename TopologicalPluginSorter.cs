using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Scotland2_Mono.Loader;

namespace Scotland2_Mono;

/// <summary>
/// Provides topological sorting for plugins based on their DLL dependencies.
/// </summary>
public static class TopologicalPluginSorter
{
    /// <summary>
    /// Sorts plugins in dependency order using topological sort (Kahn's algorithm).
    /// Plugins with no dependencies come first, followed by plugins that depend on them.
    /// </summary>
    /// <param name="plugins">The collection of plugins to sort.</param>
    /// <returns>A sorted list where dependencies come before dependents. Returns original order if cycles detected.</returns>
    public static IReadOnlyList<NativeBinary> SortPlugins(IEnumerable<NativeBinary> plugins)
    {
        var pluginList = plugins.ToList();

        if (pluginList.Count == 0)
        {
            return pluginList.AsReadOnly();
        }

        // Build a map of plugin name to plugin info
        var pluginMap = new Dictionary<string, NativeBinary>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var plugin in pluginList)
        {
            pluginMap[plugin.Name] = plugin;
        }

        // Build dependency graph: key depends on values
        // Also normalize dependency names to match plugin names (remove extensions, paths)
        var dependencyGraph = new Dictionary<string, HashSet<string>>(System.StringComparer.OrdinalIgnoreCase);
        var inDegree = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

        // Initialize
        foreach (var plugin in pluginList)
        {
            dependencyGraph[plugin.Name] = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            inDegree[plugin.Name] = 0;
        }

        // Build the graph
        foreach (var plugin in pluginList)
        {
            if (plugin.Dependencies is null) continue;
            
            foreach (var dep in plugin.Dependencies)
            {
                // Normalize dependency name (remove extension and path)
                var depName = Path.GetFileNameWithoutExtension(dep);

                // Only consider dependencies that are in our plugin set
                if (pluginMap.ContainsKey(depName))
                {
                    dependencyGraph[plugin.Name].Add(depName);
                }
            }
        }

        // Calculate in-degrees (how many plugins depend on each plugin)
        foreach (var plugin in pluginList)
        {
            foreach (var dep in dependencyGraph[plugin.Name])
            {
                inDegree[dep]++;
            }
        }

        // Kahn's algorithm for topological sort
        var queue = new Queue<string>();
        var result = new List<NativeBinary>();

        // Start with plugins that have no dependents (in-degree = 0)
        // These are the "leaf" plugins that nothing else depends on
        foreach (var plugin in pluginList)
        {
            if (inDegree[plugin.Name] == 0)
            {
                queue.Enqueue(plugin.Name);
            }
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(pluginMap[current]);

            // For each dependency of current plugin
            foreach (var dep in dependencyGraph[current])
            {
                inDegree[dep]--;

                // If dependency now has no dependents, add to queue
                if (inDegree[dep] == 0)
                {
                    queue.Enqueue(dep);
                }
            }
        }

        // Check for cycles
        if (result.Count != pluginList.Count)
        {
            Plugin.Log.Warn($"Circular dependency detected in plugins. Unable to determine optimal load order.");
            Plugin.Log.Warn(
                $"Sorted {result.Count}/{pluginList.Count} plugins. Remaining plugins may have circular dependencies.");

            // Add remaining plugins in original order
            var sortedNames = new HashSet<string>(result.Select(p => p.Name), System.StringComparer.OrdinalIgnoreCase);
            foreach (var plugin in pluginList)
            {
                if (!sortedNames.Contains(plugin.Name))
                {
                    result.Add(plugin);
                }
            }
        }

        // Reverse the result since we want dependencies first
        // (Kahn's algorithm gives us reverse topological order in this case)
        result.Reverse();

        return result.AsReadOnly();
    }

    /// <summary>
    /// Validates the plugin dependency graph for cycles and missing dependencies.
    /// </summary>
    /// <param name="plugins">The collection of plugins to validate.</param>
    /// <param name="errors">List of error messages describing validation failures.</param>
    /// <returns>True if the dependency graph is valid, false if cycles or missing dependencies exist.</returns>
    public static bool ValidateDependencies(IEnumerable<NativeBinary> plugins, out List<string> errors)
    {
        errors = new List<string>();
        var pluginList = plugins.ToList();

        if (pluginList.Count == 0)
            return true;

        var pluginNames = new HashSet<string>(
            pluginList.Select(p => p.Name),
            System.StringComparer.OrdinalIgnoreCase);

        // Check for missing dependencies
        foreach (var plugin in pluginList)
        {
            if (plugin.Dependencies is null) continue;
            
            foreach (var dep in plugin.Dependencies)
            {
                var depName = Path.GetFileNameWithoutExtension(dep);

                // Check if this dependency is another plugin we're loading
                if (pluginNames.Contains(depName))
                {
                    continue; // Dependency is satisfied
                }

                // Check if it's a system library (common system DLLs)
                if (IsSystemLibrary(dep))
                {
                    continue; // System dependency, not a concern
                }

                // Missing dependency
                errors.Add($"Plugin '{plugin.Name}' has unsatisfied dependency: '{dep}'");
            }
        }

        // Check for cycles using DFS
        var sorted = SortPlugins(pluginList);
        if (sorted.Count != pluginList.Count)
        {
            errors.Add("Circular dependency detected in plugin graph");
            return false;
        }

        return errors.Count == 0;
    }

    /// <summary>
    /// Determines if a library name is a common system library that doesn't need to be in the plugin set.
    /// </summary>
    private static bool IsSystemLibrary(string libraryName)
    {
        var name = libraryName.ToLowerInvariant();

        // Windows system libraries
        if (name.Contains("kernel32") || name.Contains("msvcr") || name.Contains("msvcp") ||
            name.Contains("ucrtbase") || name.Contains("user32") || name.Contains("advapi32") ||
            name.Contains("ws2_32") || name.Contains("shell32") || name.Contains("ole32") ||
            name.Contains("vcruntime") || name.Contains("api-ms-win"))
        {
            return true;
        }

        // Linux system libraries
        if (name.Contains("libc.so") || name.Contains("libpthread") || name.Contains("libdl.so") ||
            name.Contains("libm.so") || name.Contains("librt.so") || name.Contains("libgcc") ||
            name.Contains("libstdc++") || name.Contains("ld-linux"))
        {
            return true;
        }

        // macOS system libraries
        if (name.Contains("libsystem") || name.Contains("libc++") || name.Contains("libobjc") ||
            name.Contains("foundation") || name.Contains("coreservices"))
        {
            return true;
        }

        return false;
    }

    public static bool ValidateDependencies(IEnumerable<NativePluginInfo> loadedPlugins, out List<string> errors)
    {
        var binaries = loadedPlugins.Select(p => p.Binary);

        return ValidateDependencies(binaries, out errors);
    }

    public static IReadOnlyList<NativeBinary> SortPlugins(IEnumerable<NativePluginInfo> loadedPlugins)
    {
        var binaries = loadedPlugins.Select(p => p.Binary);

        return SortPlugins(binaries);
    }
}

