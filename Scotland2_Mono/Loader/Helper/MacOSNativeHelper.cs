using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Scotland2_Mono.Loader.Helper;

/// <summary>
/// macOS implementation of native library operations.
/// </summary>
internal class MacOSNativeHelper : UnixNativeHelper
{
    public override List<string> GetDependencies(string libraryPath)
    {
        var dependencies = new List<string>();

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "otool",
                Arguments = $"-L \"{libraryPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    var lines = output.Split('\n');
                    bool firstLine = true;
                    foreach (var line in lines)
                    {
                        if (firstLine)
                        {
                            firstLine = false;
                            continue; // Skip the first line (file name)
                        }

                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        // Parse otool output format: "    /path/to/lib.dylib (compatibility version x.x.x, current version x.x.x)"
                        var trimmed = line.Trim();
                        var spaceIndex = trimmed.IndexOf(' ');
                        if (spaceIndex > 0)
                        {
                            var libPath = trimmed.Substring(0, spaceIndex);
                            var libName = Path.GetFileName(libPath);
                            dependencies.Add(libName);
                        }
                    }
                }
            }
        }
        catch
        {
            // If otool fails, return empty list
        }

        return dependencies;
    }
}


