using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Scotland2_Mono.Loader.Helper;

/// <summary>
/// Windows implementation of native library operations.
/// </summary>
internal class WindowsNativeHelper : INativeHelper
{
    #region P/Invoke Declarations

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern NativeLibraryHandle LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeLibrary(NativeLibraryHandle hModule);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern IntPtr GetProcAddress(NativeLibraryHandle hModule, string lpProcName);

    #endregion

    public NativeLibraryHandle LoadNativeLibrary(string libraryPath)
    {
        return LoadLibrary(libraryPath);
    }

    public bool UnloadNativeLibrary(NativeLibraryHandle handle)
    {
        if (handle == IntPtr.Zero)
            return false;

        return FreeLibrary(handle);
    }

    public IntPtr GetFunctionPointer(NativeLibraryHandle handle, string functionName)
    {
        if (handle == IntPtr.Zero)
            return IntPtr.Zero;

        return GetProcAddress(handle, functionName);
    }

    public string GetLastError()
    {
        int errorCode = Marshal.GetLastWin32Error();
        return $"Win32 Error {errorCode}";
    }

   /// <summary>
    /// Gets dependencies for a Windows PE/DLL file by parsing the import table.
    /// </summary>
    public List<string> GetDependencies(string dllPath)
    {
        var dependencies = new List<string>();

        if (!File.Exists(dllPath))
            return dependencies;

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
}

