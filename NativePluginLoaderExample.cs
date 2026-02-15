using System;
using System.IO;
using System.Runtime.InteropServices;
using Scotland2_Mono.Loader;

namespace Scotland2_Mono;

/// <summary>
/// Example usage of the NativePluginLoader class for loading native (unmanaged) DLLs.
/// </summary>
public static class NativePluginLoaderExample
{
    // Example delegate for calling native functions
    // This should match the signature of the native function you want to call
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int MyFunctionDelegate(int a, int b);

    /// <summary>
    /// Example of loading native plugins from a directory.
    /// </summary>
    public static void LoadPluginsExample()
    {
        // Create a plugin loader for a specific directory
        string pluginDir = Path.Combine(Environment.CurrentDirectory, "NativePlugins");
        var loader = new NativePluginLoader(pluginDir);

        // Load all DLL files from the directory
        int loadedCount = loader.LoadPlugins();
        Plugin.Log.Info($"Loaded {loadedCount} native plugins");

        // Get summary
        Plugin.Log.Info(loader.GetSummary());

        // Iterate through all loaded plugins
        foreach (var pluginInfo in loader.GetLoadedPlugins())
        {
            Plugin.Log.Info($"  - {pluginInfo.Name} (Handle: 0x{pluginInfo.LibraryHandle:X})");
        }

        // Check for failed plugins
        foreach (var pluginInfo in loader.GetFailedPlugins())
        {
            Plugin.Log.Warn($"  - Failed: {pluginInfo.Name} - {pluginInfo.ErrorMessage}");
        }

        // Get a specific plugin by name
        var specificPlugin = loader.GetPluginByName("MyNativePlugin");
        if (specificPlugin != null && specificPlugin.IsLoaded)
        {
            Plugin.Log.Info($"Found plugin: {specificPlugin}");
            
            // You can get function pointers from the loaded library
            IntPtr functionPtr = NativeLoaderHelper.GetFunctionPointer(specificPlugin.LibraryHandle, "MyExportedFunction");
            if (functionPtr != IntPtr.Zero)
            {
                Plugin.Log.Info($"Found function at: 0x{functionPtr:X}");
                
                // Example: Call the function using delegates
                // var myFunction = Marshal.GetDelegateForFunctionPointer<MyFunctionDelegate>(functionPtr);
                // myFunction();
            }
        }
    }

    /// <summary>
    /// Example of loading plugins from subdirectories as well.
    /// </summary>
    public static void LoadPluginsRecursiveExample()
    {
        string pluginDir = Path.Combine(Environment.CurrentDirectory, "NativePlugins");
        var loader = new NativePluginLoader(pluginDir);

        // Load all DLL files recursively from subdirectories
        int loadedCount = loader.LoadPlugins("*.dll", SearchOption.AllDirectories);
        Plugin.Log.Info($"Loaded {loadedCount} native plugins from {pluginDir} and subdirectories");
    }

    /// <summary>
    /// Example of loading a single plugin file.
    /// </summary>
    public static void LoadSinglePluginExample()
    {
        string pluginDir = Path.Combine(Environment.CurrentDirectory, "NativePlugins");
        var loader = new NativePluginLoader(pluginDir);

        // Load a single DLL file
        string dllPath = Path.Combine(pluginDir, "MyNativePlugin.dll");
        bool success = loader.LoadPlugin(dllPath);

        if (success)
        {
            Plugin.Log.Info($"Successfully loaded native plugin from {dllPath}");
        }
        else
        {
            Plugin.Log.Error($"Failed to load native plugin from {dllPath}");
        }
    }

    /// <summary>
    /// Example of calling a function from a native plugin using delegates.
    /// </summary>
    public static void CallNativeFunctionExample()
    {
        string pluginDir = Path.Combine(Environment.CurrentDirectory, "NativePlugins");
        var loader = new NativePluginLoader(pluginDir);
        loader.LoadPlugins();

        var plugin = loader.GetPluginByName("MyNativePlugin");
        if (plugin != null && plugin.IsLoaded)
        {
            // Get the function pointer
            IntPtr funcPtr = NativeLoaderHelper.GetFunctionPointer(plugin.LibraryHandle, "MyFunction");
            
            if (funcPtr != IntPtr.Zero)
            {
                // Convert the function pointer to a delegate
                var myFunction = Marshal.GetDelegateForFunctionPointer<MyFunctionDelegate>(funcPtr);
                
                // Call the native function
                int result = myFunction(10, 20);
                Plugin.Log.Info($"Native function returned: {result}");
            }
        }
    }

    /// <summary>
    /// Example of properly unloading all native plugins when done.
    /// </summary>
    public static void UnloadPluginsExample()
    {
        string pluginDir = Path.Combine(Environment.CurrentDirectory, "NativePlugins");
        var loader = new NativePluginLoader(pluginDir);
        
        loader.LoadPlugins();
        
        // Do work with plugins...
        
        // When done, unload all native libraries
        loader.UnloadAll();
        Plugin.Log.Info("All native plugins unloaded");
    }
}

