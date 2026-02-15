using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Scotland2_Mono.Loader;

/// <summary>
/// Contains information about a loaded native plugin (after loading).
/// Created from NativeBinary when successfully loaded into memory.
/// </summary>
public class NativePluginInfo
{
    /// <summary>
    /// The underlying binary metadata.
    /// </summary>
    public NativeBinary Binary { get; }

    /// <summary>
    /// The handle to the loaded native library.
    /// </summary>
    public NativeLibraryHandle LibraryHandle { get; }

    /// <summary>
    /// The name of the library (filename without extension).
    /// </summary>
    public string Name => Binary.Name;

    /// <summary>
    /// Error message if loading failed, null otherwise.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Whether the plugin was successfully loaded.
    /// </summary>
    public bool IsLoaded { get; }

    /// <summary>
    /// The time when the plugin was loaded.
    /// </summary>
    public DateTime LoadedAt { get; }

    /// <summary>
    /// Dependencies from the binary metadata.
    /// </summary>
    public IReadOnlyList<string>? Dependencies => Binary.Dependencies;

    // Private constructor
    private NativePluginInfo(NativeBinary binary, NativeLibraryHandle handle, bool isLoaded, string? error)
    {
        Binary = binary;
        LibraryHandle = handle;
        IsLoaded = isLoaded;
        ErrorMessage = error;
        LoadedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a successful plugin info from a loaded binary.
    /// </summary>
    public static NativePluginInfo Loaded(NativeBinary binary, NativeLibraryHandle handle)
    {
        return new NativePluginInfo(binary, handle, true, null);
    }

    /// <summary>
    /// Creates a failed plugin info for an unloaded binary.
    /// </summary>
    public static NativePluginInfo Error(NativeBinary binary, string errorMessage)
    {
        return new NativePluginInfo(binary, NativeLibraryHandle.Null, false, errorMessage);
    }

    #region Native Function Calling Delegates

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SetupDelegate(IntPtr modInfoRef);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void LoadDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void LateLoadDelegate();

    #endregion

    /// <summary>
    /// Calls the "setup" function in the loaded library if it exists.
    /// </summary>
    public void CallSetup()
    {
        if (!IsLoaded || LibraryHandle == NativeLibraryHandle.Null)
            return;

        Plugin.Log.Info($"Attempting to call setup function in {Name}...");
        try
        {
            var setupFuncPtr = NativeLoaderHelper.GetFunctionPointer(LibraryHandle, "setup");
            if (setupFuncPtr == IntPtr.Zero)
            {
                Plugin.Log.Debug($"No setup function found in {Name}.");
                return;
            }

            Plugin.Log.Info($"Calling setup function in {Name}...");
            var setupDelegate = Marshal.GetDelegateForFunctionPointer<SetupDelegate>(setupFuncPtr);
            setupDelegate(LibraryHandle);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error calling setup in {Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Calls the "load" function in the loaded library if it exists.
    /// </summary>
    public void CallLoad()
    {
        if (!IsLoaded || LibraryHandle == NativeLibraryHandle.Null)
            return;

        Plugin.Log.Info($"Attempting to load function in {Name}...");
        try
        {
            var loadFuncPtr = NativeLoaderHelper.GetFunctionPointer(LibraryHandle, "load");
            if (loadFuncPtr == IntPtr.Zero)
            {
                Plugin.Log.Debug($"No load function found in {Name}.");
                return;
            }

            Plugin.Log.Info($"Calling load function in {Name}...");
            var loadDelegate = Marshal.GetDelegateForFunctionPointer<LoadDelegate>(loadFuncPtr);
            loadDelegate();
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error calling load in {Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Calls the "late_load" function in the loaded library if it exists.
    /// </summary>
    public void CallLateLoad()
    {
        if (!IsLoaded || LibraryHandle == NativeLibraryHandle.Null)
            return;

        Plugin.Log.Info($"Calling late_load function in {Name}...");
        try
        {
            var lateLoadFuncPtr = NativeLoaderHelper.GetFunctionPointer(LibraryHandle, "late_load");
            if (lateLoadFuncPtr == IntPtr.Zero)
            {
                Plugin.Log.Debug($"No late_load function found in {Name}.");
                return;
            }

            Plugin.Log.Info($"Calling late_load function in {Name}...");
            var lateLoadDelegate = Marshal.GetDelegateForFunctionPointer<LateLoadDelegate>(lateLoadFuncPtr);
            lateLoadDelegate();
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error calling late_load in {Name}: {ex.Message}");
        }
    }

    public override string ToString()
    {
        if (IsLoaded)
        {
            return $"{Name} (Handle: 0x{LibraryHandle:X}) - Loaded at {LoadedAt:yyyy-MM-dd HH:mm:ss} ({Binary.FilePath})";
        }

        return $"{Name} (Failed: {ErrorMessage}) - Attempted at {LoadedAt:yyyy-MM-dd HH:mm:ss}";
    }
}

