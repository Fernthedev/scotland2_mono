using System;
using System.IO;
using NUnit.Framework;
using Scotland2_Mono.Loader;
using Semver;

namespace Scotland2_Mono.Tests;

/// <summary>
/// Integration tests for NativePluginLoader with ModloaderExports.
/// Tests the integration between the loader and the export API.
/// </summary>
[TestFixture]
public class NativePluginLoaderIntegrationTests
{
    private NativePluginLoader _loader = null!;
    private string _testDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        _loader = new NativePluginLoader();
        _testDirectory = Path.Combine(Path.GetTempPath(), "Scotland2Tests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDirectory);

        ModloaderExports.Initialize(
            pluginLoader: _loader,
            modloaderPath: "/test/libsl2.so",
            rootLoadPath: "/test/modloader",
            filesDir: "/test/files",
            externalDir: "/test/external",
            applicationId: "com.test.app",
            sourcePath: "/test/source",
            libil2cppPath: "/test/libil2cpp.so"
        );
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Test]
    public void LoadPlugin_SuccessfulLoad_RegistersInExports()
    {
        // Arrange
        var binary = new NativeBinary(Path.Combine(_testDirectory, "test_plugin.dll"));
        
        // Create a mock NativePluginInfo
        var pluginInfo = NativePluginInfo.Loaded(binary, new NativeLibraryHandle(IntPtr.Zero));
        pluginInfo.Id = "TestPlugin";
        pluginInfo.Version = SemVersion.Parse("1.0.0");
        pluginInfo.VersionLong = 0x010000FF;

        // Act
        ModloaderExports.RegisterModule(pluginInfo);
        var loaded = ModloaderExports.modloader_get_loaded();

        // Assert
        Assert.That(loaded.Size, Is.EqualTo(1));
    }

    [Test]
    public void MultiplePlugins_AllLoadedAndQueried()
    {
        // Arrange
        var plugins = new[]
        {
            CreateTestPlugin("Plugin1", "1.0.0"),
            CreateTestPlugin("Plugin2", "2.0.0"),
            CreateTestPlugin("Plugin3", "3.0.0")
        };

        // Act
        foreach (var plugin in plugins)
        {
            ModloaderExports.RegisterModule(plugin);
        }

        // Query each one
        var searchInfo1 = new CModInfo { Id = "Plugin1" };
        var result1 = ModloaderExports.modloader_get_mod(ref searchInfo1, CMatchType.MatchType_IdOnly);

        var searchInfo2 = new CModInfo { Id = "Plugin2" };
        var result2 = ModloaderExports.modloader_get_mod(ref searchInfo2, CMatchType.MatchType_IdOnly);

        var searchInfo3 = new CModInfo { Id = "Plugin3" };
        var result3 = ModloaderExports.modloader_get_mod(ref searchInfo3, CMatchType.MatchType_IdOnly);

        // Assert
        Assert.That(result1.Info.Id, Is.EqualTo("Plugin1"));
        Assert.That(result2.Info.Id, Is.EqualTo("Plugin2"));
        Assert.That(result3.Info.Id, Is.EqualTo("Plugin3"));
    }

    [Test]
    public void PluginUnload_RemovesFromExports()
    {
        // Arrange
        var plugin = CreateTestPlugin("UnloadablePlugin", "1.0.0");
        ModloaderExports.RegisterModule(plugin);

        var searchInfo = new CModInfo { Id = "UnloadablePlugin" };

        // Act
        ModloaderExports.modloader_force_unload(searchInfo, CMatchType.MatchType_IdOnly);
        var loaded = ModloaderExports.modloader_get_loaded();

        // Assert
        Assert.That(loaded.Size, Is.EqualTo(0));
    }

    [Test]
    public void FailedPlugin_RegisteredAndRetrieved()
    {
        // Arrange
        var failedPlugin = CreateFailedTestPlugin("FailedPlugin", "Critical load error");

        // Act
        ModloaderExports.RegisterFailedModule(failedPlugin);
        var all = ModloaderExports.modloader_get_all();

        // Assert
        Assert.That(all.Size, Is.EqualTo(1));
    }

    [Test]
    public void MixedSuccessAndFailure_BothTracked()
    {
        // Arrange
        var successPlugin = CreateTestPlugin("SuccessPlugin", "1.0.0");
        var failedPlugin = CreateFailedTestPlugin("FailedPlugin", "Load failed");

        // Act
        ModloaderExports.RegisterModule(successPlugin);
        ModloaderExports.RegisterFailedModule(failedPlugin);

        var loaded = ModloaderExports.modloader_get_loaded();
        var all = ModloaderExports.modloader_get_all();

        // Assert
        Assert.That(loaded.Size, Is.EqualTo(1));
        Assert.That(all.Size, Is.EqualTo(2));
        
        // Cleanup
        ModloaderExports.modloader_free_results(ref loaded);
        FreeLoadResults(all);
    }

    #region Helper Methods

    private NativePluginInfo CreateTestPlugin(string id, string version, ulong versionLong = 0)
    {
        var binary = new NativeBinary(Path.Combine(_testDirectory, $"{id}.dll"));
        var info = NativePluginInfo.Loaded(binary, new NativeLibraryHandle(IntPtr.Zero));
        info.Version = SemVersion.Parse(version);
        info.VersionLong = versionLong;
        return info;
    }

    private NativePluginInfo CreateFailedTestPlugin(string id, string errorMessage)
    {
        var binary = new NativeBinary(Path.Combine(_testDirectory, $"{id}_failed.dll"));
        return NativePluginInfo.Error(binary, errorMessage);
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
/// Tests for edge cases and error handling in ModloaderExports.
/// </summary>
[TestFixture]
public class ModloaderExportsEdgeCaseTests
{
    [SetUp]
    public void SetUp()
    {
        ModloaderExports.Initialize(
            pluginLoader: new NativePluginLoader(),
            modloaderPath: "/test/libsl2.so",
            rootLoadPath: "/test/modloader",
            filesDir: "/test/files",
            externalDir: "/test/external",
            applicationId: "com.test.app",
            sourcePath: "/test/source",
            libil2cppPath: "/test/libil2cpp.so"
        );
    }

    [Test]
    public void QueryEmpty_ReturnsEmpty()
    {
        // Act
        var result = ModloaderExports.modloader_get_loaded();

        // Assert
        Assert.That(result.Size, Is.EqualTo(0));
        Assert.That(result.Array, Is.EqualTo(IntPtr.Zero));
    }

    [Test]
    public void MatchType_ObjectName_WithFilePath()
    {
        // Arrange
        var testDir = Path.GetTempPath();
        var binary = new NativeBinary(Path.Combine(testDir, "libmymod.so"));
        var plugin = NativePluginInfo.Loaded(binary, new NativeLibraryHandle(IntPtr.Zero));
        
        ModloaderExports.RegisterModule(plugin);

        var searchInfo = new CModInfo { Id = "libmymod.so" };

        // Act
        var result = ModloaderExports.modloader_get_mod(ref searchInfo, CMatchType.MatchType_ObjectName);

        // Assert
        Assert.That(result.Info.Id, Is.EqualTo("libmymod"));
    }

    [Test]
    public void StateVariables_CanBeModified()
    {
        // Arrange
        var initialPhase = ModloaderExports.CurrentLoadPhase;

        // Act
        ModloaderExports.CurrentLoadPhase = CLoadPhase.LoadPhase_Mods;
        ModloaderExports.LibsOpened = true;
        ModloaderExports.EarlyModsOpened = true;

        // Assert
        Assert.That(ModloaderExports.CurrentLoadPhase, Is.EqualTo(CLoadPhase.LoadPhase_Mods));
        Assert.That(ModloaderExports.LibsOpened, Is.True);
        Assert.That(ModloaderExports.EarlyModsOpened, Is.True);
    }

    [Test]
    public void EnvironmentVariable_LD_LIBRARY_PATH_CanBeSet()
    {
        // Arrange
        string testPath = "/test/lib/path";
        string originalValue = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH") ?? string.Empty;

        try
        {
            // Clear it first
            Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", "");

            // Act
            ModloaderExports.modloader_add_ld_library_path(testPath);
            string newValue = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH") ?? string.Empty;

            // Assert
            Assert.That(newValue, Does.Contain(testPath));
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", originalValue);
        }
    }

    [Test]
    public void DuplicateModule_SecondRegistrationAddsNew()
    {
        // Arrange
        var testDir = Path.GetTempPath();
        var plugin1 = CreatePlugin("DuplicateMod", "1.0.0", testDir);
        var plugin2 = CreatePlugin("DuplicateMod", "1.0.0", testDir);

        // Act
        ModloaderExports.RegisterModule(plugin1);
        ModloaderExports.RegisterModule(plugin2);
        var loaded = ModloaderExports.modloader_get_loaded();

        // Assert
        Assert.That(loaded.Size, Is.EqualTo(2));
        
        // Cleanup
        ModloaderExports.modloader_free_results(ref loaded);
    }

    [Test]
    public void Strict_Matching_RequiresAllFields()
    {
        // Arrange
        var testDir = Path.GetTempPath();
        var plugin = CreatePlugin("StrictMod", "2.5.3", testDir, 0x020503FF);

        ModloaderExports.RegisterModule(plugin);

        // Match with all fields
        var searchAllMatch = new CModInfo { Id = "StrictMod", Version = "2.5.3", VersionLong = 0x020503FF };
        var resultAllMatch = ModloaderExports.modloader_get_mod(ref searchAllMatch, CMatchType.MatchType_Strict);

        // Match with wrong version long
        var searchWrongLong = new CModInfo { Id = "StrictMod", Version = "2.5.3", VersionLong = 0x000000FF };
        var resultWrongLong = ModloaderExports.modloader_get_mod(ref searchWrongLong, CMatchType.MatchType_Strict);

        // Assert
        Assert.That(resultAllMatch.Info.Id, Is.EqualTo("StrictMod"));
        Assert.That(resultWrongLong.Path, Is.Null);
    }

    #region Helper Methods

    private NativePluginInfo CreatePlugin(string id, string version, string testDir, ulong versionLong = 0)
    {
        var binary = new NativeBinary(Path.Combine(testDir, $"{id}_{Guid.NewGuid()}.dll"));
        var plugin = NativePluginInfo.Loaded(binary, new NativeLibraryHandle(IntPtr.Zero));
        plugin.Version = SemVersion.Parse(version);
        plugin.VersionLong = versionLong;
        return plugin;
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

