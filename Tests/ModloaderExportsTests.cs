using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Scotland2_Mono.Loader;
using Semver;

namespace Scotland2_Mono.Tests;

/// <summary>
/// Unit tests for the ModloaderExports API.
/// Tests the C API surface exposed to unmanaged code.
/// </summary>
[TestFixture]
public class ModloaderExportsTests
{
    private NativePluginLoader _pluginLoader = null!;

    [SetUp]
    public void SetUp()
    {
        _pluginLoader = new NativePluginLoader();
        
        // Initialize the modloader with test data
        ModloaderExports.Initialize(
            pluginLoader: _pluginLoader,
            modloaderPath: "/data/user/0/com.beatgames.beatsaber/files/libsl2.so",
            rootLoadPath: "/sdcard/ModData/com.beatgames.beatsaber/Modloader",
            filesDir: "/data/user/0/com.beatgames.beatsaber/files",
            externalDir: "/storage/emulated/0/Android/data/com.beatgames.beatsaber/files",
            applicationId: "com.beatgames.beatsaber",
            sourcePath: "/sdcard/ModData/com.beatgames.beatsaber/Modloader/libsl2.so",
            libil2cppPath: "/data/user/0/com.beatgames.beatsaber/files/libil2cpp.so"
        );
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up any registered modules
        ClearModules();
    }

    #region Query Functions Tests

    [Test]
    public void modloader_get_failed_ReturnsFalseInitially()
    {
        // Act
        bool result = ModloaderExports.modloader_get_failed();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void modloader_get_path_ReturnsCorrectPath()
    {
        // Act
        string result = ModloaderExports.modloader_get_path();

        // Assert
        Assert.That(result, Is.EqualTo("/data/user/0/com.beatgames.beatsaber/files/libsl2.so"));
    }

    [Test]
    public void modloader_get_root_load_path_ReturnsCorrectPath()
    {
        // Act
        string result = ModloaderExports.modloader_get_root_load_path();

        // Assert
        Assert.That(result, Is.EqualTo("/sdcard/ModData/com.beatgames.beatsaber/Modloader"));
    }

    [Test]
    public void modloader_get_files_dir_ReturnsCorrectPath()
    {
        // Act
        string result = ModloaderExports.modloader_get_files_dir();

        // Assert
        Assert.That(result, Is.EqualTo("/data/user/0/com.beatgames.beatsaber/files"));
    }

    [Test]
    public void modloader_get_external_dir_ReturnsCorrectPath()
    {
        // Act
        string result = ModloaderExports.modloader_get_external_dir();

        // Assert
        Assert.That(result, Is.EqualTo("/storage/emulated/0/Android/data/com.beatgames.beatsaber/files"));
    }

    [Test]
    public void modloader_get_application_id_ReturnsCorrectId()
    {
        // Act
        string result = ModloaderExports.modloader_get_application_id();

        // Assert
        Assert.That(result, Is.EqualTo("com.beatgames.beatsaber"));
    }

    [Test]
    public void modloader_get_source_path_ReturnsCorrectPath()
    {
        // Act
        string result = ModloaderExports.modloader_get_source_path();

        // Assert
        Assert.That(result, Is.EqualTo("/sdcard/ModData/com.beatgames.beatsaber/Modloader/libsl2.so"));
    }

    [Test]
    public void modloader_get_libil2cpp_path_ReturnsCorrectPath()
    {
        // Act
        string result = ModloaderExports.modloader_get_libil2cpp_path();

        // Assert
        Assert.That(result, Is.EqualTo("/data/user/0/com.beatgames.beatsaber/files/libil2cpp.so"));
    }

    #endregion

    #region Module Registration Tests

    [Test]
    public void RegisterModule_AddsModuleToList()
    {
        // Arrange
        var pluginInfo = CreateTestPluginInfo("TestMod", "1.0.0");

        // Act
        ModloaderExports.RegisterModule(pluginInfo);
        var result = ModloaderExports.modloader_get_loaded();

        // Assert
        Assert.That(result.Size, Is.EqualTo(1));
    }

    [Test]
    public void RegisterFailedModule_AddsFailedModuleToList()
    {
        // Arrange
        var pluginInfo = CreateFailedTestPluginInfo("FailedMod", "Failed to load");

        // Act
        ModloaderExports.RegisterFailedModule(pluginInfo);
        var result = ModloaderExports.modloader_get_all();

        // Assert
        Assert.That(result.Size, Is.EqualTo(1));
    }

    [Test]
    public void RegisterModule_MultipleModules_AllRegistered()
    {
        // Arrange
        var mod1 = CreateTestPluginInfo("Mod1", "1.0.0");
        var mod2 = CreateTestPluginInfo("Mod2", "2.0.0");
        var mod3 = CreateTestPluginInfo("Mod3", "3.0.0");

        // Act
        ModloaderExports.RegisterModule(mod1);
        ModloaderExports.RegisterModule(mod2);
        ModloaderExports.RegisterModule(mod3);
        var result = ModloaderExports.modloader_get_loaded();

        // Assert
        Assert.That(result.Size, Is.EqualTo(3));
    }

    #endregion

    #region Mod Query Tests

    [Test]
    public void modloader_get_mod_ByIdOnly_FindsModule()
    {
        // Arrange
        var pluginInfo = CreateTestPluginInfo("MyMod", "1.5.0");
        ModloaderExports.RegisterModule(pluginInfo);

        var searchInfo = new CModInfo { Id = "MyMod" };

        // Act
        var result = ModloaderExports.modloader_get_mod(ref searchInfo, CMatchType.MatchType_IdOnly);

        // Assert
        Assert.That(result.Info.Id, Is.EqualTo("MyMod"));
        Assert.That(result.Path, Is.Not.Null);
    }

    [Test]
    public void modloader_get_mod_ByIdVersion_FindsModule()
    {
        // Arrange
        var pluginInfo = CreateTestPluginInfo("VersionedMod", "2.3.4");
        ModloaderExports.RegisterModule(pluginInfo);

        var searchInfo = new CModInfo { Id = "VersionedMod", Version = "2.3.4" };

        // Act
        var result = ModloaderExports.modloader_get_mod(ref searchInfo, CMatchType.MatchType_IdVersion);

        // Assert
        Assert.That(result.Info.Id, Is.EqualTo("VersionedMod"));
    }

    [Test]
    public void modloader_get_mod_WithWrongVersion_DoesNotFind()
    {
        // Arrange
        var pluginInfo = CreateTestPluginInfo("VersionedMod", "2.3.4");
        ModloaderExports.RegisterModule(pluginInfo);

        var searchInfo = new CModInfo { Id = "VersionedMod", Version = "1.0.0" };

        // Act
        var result = ModloaderExports.modloader_get_mod(ref searchInfo, CMatchType.MatchType_IdVersion);

        // Assert
        Assert.That(result.Path, Is.Null);
    }

    [Test]
    public void modloader_get_mod_ByIdVersionLong_FindsModule()
    {
        // Arrange
        var pluginInfo = CreateTestPluginInfo("LongVersionMod", "1.0.0", versionLong: 0x010000FF);
        ModloaderExports.RegisterModule(pluginInfo);

        var searchInfo = new CModInfo { Id = "LongVersionMod", VersionLong = 0x010000FF };

        // Act
        var result = ModloaderExports.modloader_get_mod(ref searchInfo, CMatchType.MatchType_IdVersionLong);

        // Assert
        Assert.That(result.Info.Id, Is.EqualTo("LongVersionMod"));
    }

    [Test]
    public void modloader_get_mod_NotFound_ReturnsEmpty()
    {
        // Arrange
        var searchInfo = new CModInfo { Id = "NonExistent" };

        // Act
        var result = ModloaderExports.modloader_get_mod(ref searchInfo, CMatchType.MatchType_IdOnly);

        // Assert
        Assert.That(result.Path, Is.Null);
    }

    #endregion

    #region Array Operations Tests

    [Test]
    public void modloader_get_loaded_EmptyList_ReturnsZeroSize()
    {
        // Act
        var result = ModloaderExports.modloader_get_loaded();

        // Assert
        Assert.That(result.Size, Is.EqualTo(0));
        Assert.That(result.Array, Is.EqualTo(IntPtr.Zero));
    }

    [Test]
    public void modloader_get_loaded_WithModules_ReturnsArray()
    {
        // Arrange
        ModloaderExports.RegisterModule(CreateTestPluginInfo("Mod1", "1.0.0"));
        ModloaderExports.RegisterModule(CreateTestPluginInfo("Mod2", "2.0.0"));

        // Act
        var result = ModloaderExports.modloader_get_loaded();

        // Assert
        Assert.That(result.Size, Is.EqualTo(2));
        Assert.That(result.Array, Is.Not.EqualTo(IntPtr.Zero));
        
        // Cleanup
        ModloaderExports.modloader_free_results(ref result);
    }

    [Test]
    public void modloader_get_all_MixedLoadStatus_ReturnsAll()
    {
        // Arrange
        ModloaderExports.RegisterModule(CreateTestPluginInfo("LoadedMod", "1.0.0"));
        ModloaderExports.RegisterFailedModule(CreateFailedTestPluginInfo("FailedMod", "Load error"));

        // Act
        var result = ModloaderExports.modloader_get_all();

        // Assert
        Assert.That(result.Size, Is.EqualTo(2));

        // Cleanup
        FreeLoadResults(result);
    }

    [Test]
    public void modloader_free_results_ClearsPointer()
    {
        // Arrange
        ModloaderExports.RegisterModule(CreateTestPluginInfo("Mod1", "1.0.0"));
        var result = ModloaderExports.modloader_get_loaded();
        var initialPtr = result.Array;

        // Act
        ModloaderExports.modloader_free_results(ref result);

        // Assert
        Assert.That(result.Array, Is.EqualTo(IntPtr.Zero));
        Assert.That(result.Size, Is.EqualTo(0));
    }

    #endregion

    #region Unload Tests

    [Test]
    public void modloader_force_unload_RemovesModule()
    {
        // Arrange
        var pluginInfo = CreateTestPluginInfo("UnloadableMod", "1.0.0");
        ModloaderExports.RegisterModule(pluginInfo);

        var searchInfo = new CModInfo { Id = "UnloadableMod" };

        // Act
        bool result = ModloaderExports.modloader_force_unload(searchInfo, CMatchType.MatchType_IdOnly);
        var loaded = ModloaderExports.modloader_get_loaded();

        // Assert
        Assert.That(result, Is.True);
        Assert.That(loaded.Size, Is.EqualTo(0));
    }

    [Test]
    public void modloader_force_unload_NonExistent_ReturnsTrue()
    {
        // Arrange
        var searchInfo = new CModInfo { Id = "NonExistent" };

        // Act
        bool result = ModloaderExports.modloader_force_unload(searchInfo, CMatchType.MatchType_IdOnly);

        // Assert
        Assert.That(result, Is.True);
    }

    #endregion

    #region Library Path Tests

    [Test]
    public void modloader_add_ld_library_path_WithValidPath_ReturnsTrue()
    {
        // Arrange
        string testPath = "/test/library/path";
        string originalPath = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH") ?? string.Empty;

        try
        {
            // Act
            bool result = ModloaderExports.modloader_add_ld_library_path(testPath);

            // Assert
            Assert.That(result, Is.True);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", originalPath);
        }
    }

    [Test]
    public void modloader_add_ld_library_path_WithNullPath_ReturnsFalse()
    {
        // Act
        bool result = ModloaderExports.modloader_add_ld_library_path(null!);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void modloader_add_ld_library_path_WithEmptyPath_ReturnsFalse()
    {
        // Act
        bool result = ModloaderExports.modloader_add_ld_library_path(string.Empty);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region Require Mod Tests

    [Test]
    public void modloader_require_mod_WithLoadedModule_ReturnsLoaded()
    {
        // Arrange
        var pluginInfo = CreateTestPluginInfo("RequiredMod", "1.0.0");
        ModloaderExports.RegisterModule(pluginInfo);

        var searchInfo = new CModInfo { Id = "RequiredMod" };

        // Act
        var result = ModloaderExports.modloader_require_mod(ref searchInfo, CMatchType.MatchType_IdOnly);

        // Assert
        Assert.That(result, Is.EqualTo(CLoadResultEnum.MatchType_Loaded));
    }

    [Test]
    public void modloader_require_mod_WithUnloadedModule_ReturnsNotFound()
    {
        // Arrange
        var searchInfo = new CModInfo { Id = "NonExistentMod" };

        // Act
        var result = ModloaderExports.modloader_require_mod(ref searchInfo, CMatchType.MatchType_IdOnly);

        // Assert
        Assert.That(result, Is.EqualTo(CLoadResultEnum.LoadResult_NotFound));
    }

    #endregion

    #region State Tracking Tests

    [Test]
    public void ModloaderState_PropertiesInitialized()
    {
        // Assert
        Assert.That(ModloaderExports.LibsOpened, Is.False);
        Assert.That(ModloaderExports.EarlyModsOpened, Is.False);
        Assert.That(ModloaderExports.LateModsOpened, Is.False);
        Assert.That(ModloaderExports.CurrentLoadPhase, Is.EqualTo(CLoadPhase.LoadPhase_None));
    }

    [Test]
    public void ModloaderState_CanUpdateLoadPhase()
    {
        // Act
        ModloaderExports.CurrentLoadPhase = CLoadPhase.LoadPhase_Libs;
        var result = ModloaderExports.GetCurrentLoadPhase();

        // Assert
        Assert.That(result, Is.EqualTo(CLoadPhase.LoadPhase_Libs));
    }

    [Test]
    public void ModloaderState_CanSetLibsOpened()
    {
        // Act
        ModloaderExports.LibsOpened = true;
        var result = ModloaderExports.GetLibsOpened();

        // Assert
        Assert.That(result, Is.True);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a mock loaded plugin info for testing.
    /// </summary>
    private static NativePluginInfo CreateTestPluginInfo(string id, string version, ulong versionLong = 0)
    {
        var binary = new NativeBinary(Path.Combine(Path.GetTempPath(), $"{id}.dll"));
        
        // Create a loaded plugin info (success case)
        var info = NativePluginInfo.Loaded(binary, new NativeLibraryHandle(IntPtr.Zero));
        
        // Set the properties that would normally be set during setup()
        info.Version = SemVersion.Parse(version);
        info.VersionLong = versionLong;

        return info;
    }

    /// <summary>
    /// Creates a mock failed plugin info for testing.
    /// </summary>
    private static NativePluginInfo CreateFailedTestPluginInfo(string id, string errorMessage)
    {
        var binary = new NativeBinary(Path.Combine(Path.GetTempPath(), $"{id}_failed.dll"));
        
        // Create a failed plugin info
        var info = NativePluginInfo.Error(binary, errorMessage);

        return info;
    }

    /// <summary>
    /// Clears all registered modules for clean test state.
    /// </summary>
    private static void ClearModules()
    {
        // Free any unmanaged result arrays first.
        var loaded = ModloaderExports.modloader_get_loaded();
        if (loaded.Size > 0)
        {
            ModloaderExports.modloader_free_results(ref loaded);
        }

        var all = ModloaderExports.modloader_get_all();
        if (all.Size > 0)
        {
            FreeLoadResults(all);
        }

        // Reset the internal list to keep tests isolated.
        var field = typeof(ModloaderExports).GetField("_loadedModules", BindingFlags.NonPublic | BindingFlags.Static);
        if (field?.GetValue(null) is List<NativePluginInfo> modules)
        {
            modules.Clear();
        }
    }

    private static void FreeLoadResults(CLoadResults results)
    {
        if (results.Array != IntPtr.Zero)
        {
            System.Runtime.InteropServices.Marshal.FreeCoTaskMem(results.Array);
        }
    }

    #endregion
}

/// <summary>
/// Unit tests for the ModloaderExtensions helper methods.
/// </summary>
[TestFixture]
public class ModloaderExtensionsTests
{
    [Test]
    public void ToManagedArray_WithCModResults_CorrectlyMarshals()
    {
        // Arrange
        var modResult = new CModResult
        {
            Info = new CModInfo { Id = "TestMod", Version = "1.0.0" },
            Path = "/test/path",
            Handle = IntPtr.Zero
        };

        int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(CModResult));
        IntPtr arrayPtr = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(size);
        System.Runtime.InteropServices.Marshal.StructureToPtr(modResult, arrayPtr, false);

        var cmodResults = new CModResults { Array = arrayPtr, Size = 1 };

        try
        {
            // Act
            var managed = cmodResults.ToManagedArray();

            // Assert
            Assert.That(managed.Length, Is.EqualTo(1));
            Assert.That(managed[0].Info.Id, Is.EqualTo("TestMod"));
            Assert.That(managed[0].Path, Is.EqualTo("/test/path"));
        }
        finally
        {
            // Cleanup
            System.Runtime.InteropServices.Marshal.FreeCoTaskMem(arrayPtr);
        }
    }

    [Test]
    public void CreateModInfo_CreatesCorrectStructure()
    {
        // Act
        var modInfo = ModloaderExtensions.CreateModInfo("TestMod", "2.1.0", 0x020100FF);

        // Assert
        Assert.That(modInfo.Id, Is.EqualTo("TestMod"));
        Assert.That(modInfo.Version, Is.EqualTo("2.1.0"));
        Assert.That(modInfo.VersionLong, Is.EqualTo(0x020100FF));
    }
}

