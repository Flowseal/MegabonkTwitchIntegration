using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;

using Il2CppInterop.Runtime.Injection;

using UnityEngine;

namespace TwitchIntegration;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    internal static new ManualLogSource Log;
    private static GameObject twitchIntegrationObject;

    public override void Load()
    {
        Log = base.Log;

        PluginConfig.Initialize(base.Config);

        ClassInjector.RegisterTypeInIl2Cpp<TwitchIntegrationBase>();

        twitchIntegrationObject = new GameObject("TwitchIntegration");
        twitchIntegrationObject.AddComponent<TwitchIntegrationBase>();
        Object.DontDestroyOnLoad(twitchIntegrationObject);

        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    public override bool Unload()
    {
        if (twitchIntegrationObject != null)
        {
            Object.Destroy(twitchIntegrationObject);
            twitchIntegrationObject = null;
        }

        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} unloaded!");
        return true;
    }
}