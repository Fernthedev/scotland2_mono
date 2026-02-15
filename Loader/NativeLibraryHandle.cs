using System;
using System.Runtime.InteropServices;

namespace Scotland2_Mono.Loader;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct NativeLibraryHandle(IntPtr Handle)
{
    public static NativeLibraryHandle Null = new(IntPtr.Zero);
    
    public IntPtr Handle { get; } = Handle;
    public bool IsValid => Handle != IntPtr.Zero;
    public bool IsNull => Handle == IntPtr.Zero;

    public static implicit operator IntPtr(NativeLibraryHandle nativeHandle) => nativeHandle.Handle;
}