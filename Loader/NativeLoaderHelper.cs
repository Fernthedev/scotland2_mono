using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Scotland2_Mono.Loader;

/// <summary>
/// Helper class containing P/Invoke declarations for native library operations.
/// </summary>
internal static class NativeLoaderHelper
{
    // Constants
    public const int RTLD_NOW = 2;

    #region Windows P/Invoke

    /// <summary>
    /// Loads a dynamic-link library (DLL) module into the address space of the calling process.
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern NativeLibraryHandle LoadLibrary(string lpFileName);

    /// <summary>
    /// Frees the loaded dynamic-link library (DLL) module.
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool FreeLibrary(NativeLibraryHandle hModule);

    /// <summary>
    /// Retrieves the address of an exported function or variable from the specified DLL.
    /// </summary>
    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    public static extern IntPtr GetProcAddress(NativeLibraryHandle hModule, string lpProcName);

    #endregion

    #region Linux/macOS P/Invoke

    /// <summary>
    /// Opens a dynamic library and returns a handle.
    /// </summary>
    [DllImport("libdl.so.2", EntryPoint = "dlopen")]
    public static extern NativeLibraryHandle dlopen(string filename, int flags);

    /// <summary>
    /// Closes a dynamic library.
    /// </summary>
    [DllImport("libdl.so.2", EntryPoint = "dlclose")]
    public static extern int dlclose(NativeLibraryHandle handle);

    /// <summary>
    /// Returns a human-readable string describing the most recent error from dlopen, dlsym, or dlclose.
    /// </summary>
    [DllImport("libdl.so.2", EntryPoint = "dlerror")]
    public static extern NativeLibraryHandle dlerror();

    /// <summary>
    /// Obtains the address of a symbol in a dynamic library.
    /// </summary>
    [DllImport("libdl.so.2", EntryPoint = "dlsym")]
    public static extern IntPtr dlsym(NativeLibraryHandle handle, string symbol);

    #endregion

    #region Cross-Platform Helpers

    /// <summary>
    /// Loads a native library using the appropriate platform-specific method.
    /// </summary>
    /// <param name="libraryPath">The path to the native library.</param>
    /// <returns>A handle to the loaded library, or IntPtr.Zero on failure.</returns>
    public static NativeLibraryHandle LoadNativeLibrary(string libraryPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return LoadLibrary(libraryPath);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return dlopen(libraryPath, RTLD_NOW);
        }

        return NativeLibraryHandle.Null;
    }

    /// <summary>
    /// Unloads a native library using the appropriate platform-specific method.
    /// </summary>
    /// <param name="handle">The handle to the library.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public static bool UnloadNativeLibrary(NativeLibraryHandle handle)
    {
        if (handle == IntPtr.Zero)
            return false;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return FreeLibrary(handle);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return dlclose(handle) == 0;
        }

        return false;
    }

    /// <summary>
    /// Gets a function pointer from a loaded native library using the appropriate platform-specific method.
    /// </summary>
    /// <param name="handle">The library handle.</param>
    /// <param name="functionName">The name of the function.</param>
    /// <returns>A pointer to the function, or IntPtr.Zero if not found.</returns>
    public static IntPtr GetFunctionPointer(NativeLibraryHandle handle, string functionName)
    {
        if (handle == IntPtr.Zero)
            return IntPtr.Zero;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return GetProcAddress(handle, functionName);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return dlsym(handle, functionName);
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// Gets the last error message from native library operations.
    /// </summary>
    /// <returns>Error message string.</returns>
    public static string GetLastError()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            int errorCode = Marshal.GetLastWin32Error();
            return $"Win32 Error {errorCode}";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            IntPtr errorPtr = dlerror();
            if (errorPtr != IntPtr.Zero)
            {
                return Marshal.PtrToStringAnsi(errorPtr) ?? "Unknown error";
            }
        }

        return "Unknown error";
    }

    /// <summary>
    /// Gets the list of linked dependencies (imported DLLs/shared libraries) for a native library.
    /// </summary>
    /// <param name="libraryPath">The path to the native library.</param>
    /// <returns>A list of dependency names, or an empty list if unable to retrieve.</returns>
    public static List<string> GetDependencies(string libraryPath)
    {
        if (!File.Exists(libraryPath))
            return new List<string>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return GetWindowsDependencies(libraryPath);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return GetLinuxDependencies(libraryPath);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return GetMacOSDependencies(libraryPath);
        }

        return new List<string>();
    }

    /// <summary>
    /// Gets dependencies for a Windows PE/DLL file by parsing the import table.
    /// </summary>
    private static List<string> GetWindowsDependencies(string dllPath)
    {
        var dependencies = new List<string>();

        try
        {
            // Read the PE file structure
            using var fs = new FileStream(dllPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(fs);

            // Read DOS header
            fs.Seek(0, SeekOrigin.Begin);
            var dosSignature = reader.ReadUInt16();
            if (dosSignature != 0x5A4D) // "MZ"
                return dependencies;

            // Get PE header offset
            fs.Seek(0x3C, SeekOrigin.Begin);
            var peHeaderOffset = reader.ReadInt32();

            // Read PE signature
            fs.Seek(peHeaderOffset, SeekOrigin.Begin);
            var peSignature = reader.ReadUInt32();
            if (peSignature != 0x00004550) // "PE\0\0"
                return dependencies;

            // Read COFF header
            reader.ReadUInt16(); // Machine
            var numberOfSections = reader.ReadUInt16();
            reader.ReadUInt32(); // TimeDateStamp
            reader.ReadUInt32(); // PointerToSymbolTable
            reader.ReadUInt32(); // NumberOfSymbols
            var sizeOfOptionalHeader = reader.ReadUInt16();
            reader.ReadUInt16(); // Characteristics

            // Read optional header magic
            var optionalHeaderStart = fs.Position;
            var magic = reader.ReadUInt16();
            var is64Bit = magic == 0x20B;

            // Skip to DataDirectory
            if (is64Bit)
            {
                fs.Seek(optionalHeaderStart + 112, SeekOrigin.Begin);
            }
            else
            {
                fs.Seek(optionalHeaderStart + 96, SeekOrigin.Begin);
            }

            // Read Import Table RVA and Size (second entry in data directory)
            reader.ReadUInt32(); // Export table RVA
            reader.ReadUInt32(); // Export table size
            var importTableRVA = reader.ReadUInt32();
            var importTableSize = reader.ReadUInt32();

            if (importTableRVA == 0)
                return dependencies;

            // Find section containing import table
            fs.Seek(optionalHeaderStart + sizeOfOptionalHeader, SeekOrigin.Begin);
            
            uint importTableFileOffset = 0;
            for (int i = 0; i < numberOfSections; i++)
            {
                var sectionName = reader.ReadBytes(8);
                var virtualSize = reader.ReadUInt32();
                var virtualAddress = reader.ReadUInt32();
                var sizeOfRawData = reader.ReadUInt32();
                var pointerToRawData = reader.ReadUInt32();
                
                reader.ReadBytes(16); // Skip remaining section header fields

                if (importTableRVA >= virtualAddress && importTableRVA < virtualAddress + virtualSize)
                {
                    importTableFileOffset = pointerToRawData + (importTableRVA - virtualAddress);
                    break;
                }
            }

            if (importTableFileOffset == 0)
                return dependencies;

            // Read import descriptors
            fs.Seek(importTableFileOffset, SeekOrigin.Begin);

            while (true)
            {
                var importLookupTableRVA = reader.ReadUInt32();
                reader.ReadUInt32(); // TimeDateStamp
                reader.ReadUInt32(); // ForwarderChain
                var nameRVA = reader.ReadUInt32();
                reader.ReadUInt32(); // ImportAddressTableRVA

                if (nameRVA == 0)
                    break;

                // Convert RVA to file offset and read name
                long currentPos = fs.Position;
                
                // Find section for name RVA
                fs.Seek(optionalHeaderStart + sizeOfOptionalHeader, SeekOrigin.Begin);
                uint nameFileOffset = 0;
                
                for (int i = 0; i < numberOfSections; i++)
                {
                    reader.ReadBytes(8); // Section name
                    var virtualSize = reader.ReadUInt32();
                    var virtualAddress = reader.ReadUInt32();
                    var sizeOfRawData = reader.ReadUInt32();
                    var pointerToRawData = reader.ReadUInt32();
                    reader.ReadBytes(16);

                    if (nameRVA >= virtualAddress && nameRVA < virtualAddress + virtualSize)
                    {
                        nameFileOffset = pointerToRawData + (nameRVA - virtualAddress);
                        break;
                    }
                }

                if (nameFileOffset > 0)
                {
                    fs.Seek(nameFileOffset, SeekOrigin.Begin);
                    var dllNameBytes = new List<byte>();
                    byte b;
                    while ((b = reader.ReadByte()) != 0)
                    {
                        dllNameBytes.Add(b);
                    }
                    var dllName = System.Text.Encoding.ASCII.GetString(dllNameBytes.ToArray());
                    dependencies.Add(dllName);
                }

                fs.Seek(currentPos, SeekOrigin.Begin);
            }
        }
        catch
        {
            // If parsing fails, return what we have
        }

        return dependencies;
    }

    /// <summary>
    /// Gets dependencies for a Linux ELF file using ldd command.
    /// </summary>
    private static List<string> GetLinuxDependencies(string soPath)
    {
        var dependencies = new List<string>();

        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ldd",
                Arguments = $"\"{soPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
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

    /// <summary>
    /// Gets dependencies for a macOS dylib file using otool command.
    /// </summary>
    private static List<string> GetMacOSDependencies(string dylibPath)
    {
        var dependencies = new List<string>();

        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "otool",
                Arguments = $"-L \"{dylibPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
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

    #endregion
}

