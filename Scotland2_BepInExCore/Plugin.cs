using System.IO;
using BepInEx;
using BepInEx.Logging;
using Scotland2_Mono;
using Scotland2_Mono.Loader;
using UnityEngine;

namespace Scotland2_BepInExCore;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log;

    private readonly NativePluginLoader _libraryLoader = new();
    private readonly NativePluginLoader _pluginLoader = new();

    private readonly string _libraryDirectory;
    private readonly string _pluginDirectory;

    public Plugin()
    {
        // Plugin startup logic
        Log = Logger;
        StaticLog.Initialize(new BepinLogger());
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        // load in UnityDir/Native/Plugins

        _libraryDirectory = (Path.Combine(Application.persistentDataPath, "Native", "Libs"));
        _pluginDirectory = (Path.Combine(Application.persistentDataPath, "Native", "Plugins"));

        Log.LogInfo($"Loading libraries {_libraryDirectory}");
        _libraryLoader.LoadPlugins(_libraryDirectory);
    }

    private void Awake()
    {
        Log.LogInfo($"Loading plugins {_pluginDirectory}");
        _pluginLoader.LoadPlugins(_pluginDirectory);
        CallSetup();
    }


    private void Start()
    {
        Log.LogInfo("Starting plugins");
        CallLoad();
        CallLateLoad();
    }


    private void CallSetup()
    {
        // Call the setup method for each plugin
        Log.LogInfo("Calling setup for plugins");
        foreach (var plugin in _pluginLoader.PluginInfos)
        {
            plugin.CallSetup();
        }
    }

    private void CallLoad()
    {
        Log.LogInfo("Calling load for plugins");
        foreach (var plugin in _pluginLoader.PluginInfos)
        {
            plugin.CallLoad();
        }
    }


    private void CallLateLoad()
    {
        Log.LogInfo("Calling late load for plugins");
        foreach (var plugin in _pluginLoader.PluginInfos)
        {
            plugin.CallLateLoad();
        }
    }
    

}