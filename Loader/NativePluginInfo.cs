using System;
using System.IO;

namespace Scotland2_Mono.Loader;

/// <summary>
/// Contains information about a loaded native plugin.
/// </summary>
public class NativePluginInfo
{
    /// <summary>
    /// The handle to the loaded native library.
    /// </summary>
    public NativeLibraryHandle LibraryHandle { get; private set; }

    /// <summary>
    /// The file path of the DLL.
    /// </summary>
    public string FilePath { get; private set; } = null!;

    /// <summary>
    /// The name of the library (filename without extension).
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Whether the plugin was successfully loaded.
    /// </summary>
    public bool IsLoaded { get; private set; }

    /// <summary>
    /// Error message if loading failed, null otherwise.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// The time when the plugin was loaded.
    /// </summary>
    public DateTime LoadedAt { get; private set; }

    public static NativePluginInfo Loaded(NativeLibraryHandle libraryHandle, string filePath) => new()
    {
        LibraryHandle = libraryHandle,
        FilePath = filePath,
        Name = Path.GetFileNameWithoutExtension(filePath),
        IsLoaded = true,
        ErrorMessage = null,
        LoadedAt = DateTime.UtcNow,
    };


    public static NativePluginInfo Error(string filePath, string errorMessage) => new()
    {
        LibraryHandle = NativeLibraryHandle.Null,
        FilePath = filePath,
        Name = Path.GetFileNameWithoutExtension(filePath),
        IsLoaded = false,
        ErrorMessage = errorMessage,
        LoadedAt = DateTime.UtcNow,
    };

    public override string ToString()
    {
        if (IsLoaded)
        {
            return $"{Name} (Handle: 0x{LibraryHandle:X}) - Loaded at {LoadedAt:yyyy-MM-dd HH:mm:ss} ({FilePath})";
        }

        return $"{Name} (Failed: {ErrorMessage}) - Attempted at {LoadedAt:yyyy-MM-dd HH:mm:ss}";
    }
}

