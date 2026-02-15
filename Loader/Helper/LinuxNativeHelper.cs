using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Scotland2_Mono.Loader.Helper;

/// <summary>
/// Linux implementation of native library operations.
/// </summary>
internal class LinuxNativeHelper : UnixNativeHelper
{
    public override List<string> GetDependencies(string libraryPath)
    {
        var dependencies = new List<string>();

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "ldd",
                Arguments = $"\"{libraryPath}\"",
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
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        // Parse ldd output format: "libname.so => /path/to/lib (address)"
                        var parts = line.Trim().Split(new[] { " => " }, StringSplitOptions.None);
                        if (parts.Length > 0)
                        {
                            var libName = parts[0].Trim();
                            if (!string.IsNullOrEmpty(libName) && !libName.StartsWith("linux-vdso"))
                            {
                                dependencies.Add(libName);
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // If ldd fails, return empty list
        }

        return dependencies;
    }
}

