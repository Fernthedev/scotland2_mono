using System;
using System.Collections.Generic;

namespace Scotland2_Mono.Loader.Helper;

/// <summary>
/// Interface for platform-specific native library operations.
/// </summary>
internal interface INativeHelper
{
    /// <summary>
    /// Loads a native library.
    /// </summary>
    /// <param name="libraryPath">The path to the native library.</param>
    /// <returns>A handle to the loaded library, or null handle on failure.</returns>
    NativeLibraryHandle LoadNativeLibrary(string libraryPath);

    /// <summary>
    /// Unloads a native library.
    /// </summary>
    /// <param name="handle">The handle to the library.</param>
    /// <returns>True if successful, false otherwise.</returns>
    bool UnloadNativeLibrary(NativeLibraryHandle handle);

    /// <summary>
    /// Gets a function pointer from a loaded native library.
    /// </summary>
    /// <param name="handle">The library handle.</param>
    /// <param name="functionName">The name of the function.</param>
    /// <returns>A pointer to the function, or IntPtr.Zero if not found.</returns>
    IntPtr GetFunctionPointer(NativeLibraryHandle handle, string functionName);

    /// <summary>
    /// Gets the last error message from native library operations.
    /// </summary>
    /// <returns>Error message string.</returns>
    string GetLastError();

    /// <summary>
    /// Gets the list of linked dependencies for a native library.
    /// </summary>
    /// <param name="libraryPath">The path to the native library.</param>
    /// <returns>A list of dependency names, or an empty list if unable to retrieve.</returns>
    List<string> GetDependencies(string libraryPath);
}

