using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Scotland2_Mono.Loader.Helper;

namespace Scotland2_Mono.Loader;

/// <summary>
/// Factory class for creating platform-specific native library helpers.
/// </summary>
internal static class NativeLoaderHelper
{
    private static readonly object Lock = new();

    /// <summary>
    /// Gets the platform-specific native helper instance.
    /// </summary>
    public static INativeHelper Instance
    {
        get
        {
            if (field != null)
                return field;

            lock (Lock)
            {
                field ??= CreateHelper();
            }
            return field;
        }
    }

    private static INativeHelper CreateHelper()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WindowsNativeHelper();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new LinuxNativeHelper();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new MacOSNativeHelper();
        }

        throw new PlatformNotSupportedException("Current platform is not supported for native library loading.");
    }

    #region Convenience Methods

    /// <summary>
    /// Loads a native library using the appropriate platform-specific method.
    /// </summary>
    public static NativeLibraryHandle LoadNativeLibrary(string libraryPath)
    {
        return Instance.LoadNativeLibrary(libraryPath);
    }

    /// <summary>
    /// Unloads a native library using the appropriate platform-specific method.
    /// </summary>
    public static bool UnloadNativeLibrary(NativeLibraryHandle handle)
    {
        return Instance.UnloadNativeLibrary(handle);
    }

    /// <summary>
    /// Gets a function pointer from a loaded native library.
    /// </summary>
    public static IntPtr GetFunctionPointer(NativeLibraryHandle handle, string functionName)
    {
        return Instance.GetFunctionPointer(handle, functionName);
    }

    /// <summary>
    /// Gets a function pointer from a loaded native library and converts to delegate.
    /// </summary>
    public static T? GetFunctionPointer<T>(NativeLibraryHandle handle, string functionName) where T : Delegate
    {
        var ptr = Instance.GetFunctionPointer(handle, functionName);
        if (ptr == IntPtr.Zero)
        {
            return null;
        }

        return Marshal.GetDelegateForFunctionPointer<T>(ptr);
    }

    /// <summary>
    /// Gets the last error message from native library operations.
    /// </summary>
    public static string GetLastError()
    {
        return Instance.GetLastError();
    }

    /// <summary>
    /// Gets the list of linked dependencies for a native library.
    /// </summary>
    public static List<string>? GetDependencies(string libraryPath)
    {
        if (!File.Exists(libraryPath))
            return null;

        return Instance.GetDependencies(libraryPath);
    }

    #endregion
}



