using System.IO;
using IPA;
using IPA.Config.Stores;
using IPA.Loader;
using IPA.Utilities;
using Scotland2_Mono.Loader;
using IpaLogger = IPA.Logging.Logger;
using IpaConfig = IPA.Config.Config;

namespace Scotland2_Mono;

[Plugin(RuntimeOptions.DynamicInit), NoEnableDisable]
internal class Plugin
{
    internal static IpaLogger Log { get; private set; } = null!;

    private NativePluginLoader _libraryLoader;
    private NativePluginLoader _pluginLoader;
    
    
    // Methods with [Init] are called when the plugin is first loaded by IPA.
    // All the parameters are provided by IPA and are optional.
    // The constructor is called before any method with [Init]. Only use [Init] with one constructor.
    [Init]
    public Plugin(IpaLogger ipaLogger, IpaConfig ipaConfig, PluginMetadata pluginMetadata)
    {
        Log = ipaLogger;
        
        // load in UnityDir/Native/Plugins
        _libraryLoader = new NativePluginLoader(Path.Combine(UnityGame.InstallPath, "Native", "Libs"));
        _pluginLoader = new NativePluginLoader(Path.Combine(UnityGame.InstallPath, "Native", "Plugins"));


        // Creates an instance of PluginConfig used by IPA to load and store config values
        var pluginConfig = ipaConfig.Generated<PluginConfig>();

        Log.Info($"{pluginMetadata.Name} {pluginMetadata.HVersion} loading libraries {_libraryLoader.PluginDirectory}");
        _libraryLoader.LoadPlugins();
    }


    [OnEnable]
    public void OnEnable()
    {
        Log.Info("Plugin enabled, loading plugins...");
        _pluginLoader.LoadPlugins();
        Log.Info("Plugin loaded");
        
    }
}