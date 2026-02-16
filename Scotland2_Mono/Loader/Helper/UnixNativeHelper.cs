using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Scotland2_Mono.Loader.Helper;

/// <summary>
/// Base class for Unix-based (Linux/macOS) native library operations.
/// </summary>
internal abstract class UnixNativeHelper : INativeHelper
{
    protected const int RTLD_NOW = 2;

    #region P/Invoke Declarations

    [DllImport("libdl.so.2", EntryPoint = "dlopen")]
    private static extern NativeLibraryHandle dlopen(string filename, int flags);

    [DllImport("libdl.so.2", EntryPoint = "dlclose")]
    private static extern int dlclose(NativeLibraryHandle handle);

    [DllImport("libdl.so.2", EntryPoint = "dlerror")]
    private static extern IntPtr dlerror();

    [DllImport("libdl.so.2", EntryPoint = "dlsym")]
    private static extern IntPtr dlsym(NativeLibraryHandle handle, string symbol);

    #endregion

    public virtual NativeLibraryHandle LoadNativeLibrary(string libraryPath)
    {
        return dlopen(libraryPath, RTLD_NOW);
    }

    public virtual bool UnloadNativeLibrary(NativeLibraryHandle handle)
    {
        if (handle == IntPtr.Zero)
            return false;

        return dlclose(handle) == 0;
    }

    public virtual IntPtr GetFunctionPointer(NativeLibraryHandle handle, string functionName)
    {
        if (handle == IntPtr.Zero)
            return IntPtr.Zero;

        return dlsym(handle, functionName);
    }

    public virtual string GetLastError()
    {
        IntPtr errorPtr = dlerror();
        if (errorPtr != IntPtr.Zero)
        {
            return Marshal.PtrToStringAnsi(errorPtr) ?? "Unknown error";
        }

        return "Unknown error";
    }

    public abstract List<string> GetDependencies(string libraryPath);
}


