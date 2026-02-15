using System;
using System.Collections.Generic;
using System.IO;

namespace Scotland2_Mono.Loader;

/// <summary>
/// Represents metadata about a native binary before it's loaded.
/// Handles dependency scanning and stores information about the binary file itself.
/// </summary>
public class NativeBinary
{
    /// <summary>
    /// The file path to the binary.
    /// </summary>
    public string FilePath { get; private set; }

    /// <summary>
    /// The name of the binary (filename without extension).
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// List of dependencies (imported DLLs/shared libraries) required by this binary.
    /// </summary>
    public IReadOnlyList<string>? Dependencies { get; private set; }

    /// <summary>
    /// The time when dependencies were scanned.
    /// </summary>
    public DateTime? DependenciesScannedAt { get; private set; }

    /// <summary>
    /// Error message if dependency scanning failed, null otherwise.
    /// </summary>
    public string? DependencyScanError { get; private set; }

    /// <summary>
    /// Whether dependency scanning was successful.
    /// </summary>
    public bool DependenciesScanned { get; private set; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSize { get; private set; }

    /// <summary>
    /// File creation time.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// File last modified time.
    /// </summary>
    public DateTime ModifiedAt { get; private set; }

    public NativeBinary(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Binary file not found: {filePath}");

        FilePath = Path.GetFullPath(filePath);
        Name = Path.GetFileNameWithoutExtension(FilePath);
        Dependencies = NativeLoaderHelper.GetDependencies(FilePath);
        DependenciesScanned = false;

        // Get file metadata
        var fileInfo = new FileInfo(FilePath);
        FileSize = fileInfo.Length;
        CreatedAt = fileInfo.CreationTimeUtc;
        ModifiedAt = fileInfo.LastWriteTimeUtc;
    }

    public override string ToString()
    {
        if (DependenciesScanned)
        {
            return $"{Name} - {Dependencies?.Count ?? 0} dependencies (size: {FileSize} bytes, scanned: {DependenciesScannedAt:yyyy-MM-dd HH:mm:ss})";
        }

        return $"{Name} - dependencies not scanned (size: {FileSize} bytes)";
    }
}

