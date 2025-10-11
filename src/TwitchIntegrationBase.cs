#nullable disable
using Il2CppSystem;
using UnityEngine;

namespace TwitchIntegration;

public class TwitchIntegrationBase : MonoBehaviour
{
    private readonly Chat chat = new Chat();
    private readonly Bosses bosses = new Bosses();
    private readonly Nicknames nicknames = new Nicknames();
    private string lastTwitchChannel = string.Empty;

    public void Awake()
    {
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;
        Plugin.Log.LogInfo("Awake called");
    }

    public void Start()
    {
        Plugin.Log.LogInfo("Start called");

        chat.RegisterMessageCallback(bosses.ProcessNewChatMessage);
        chat.RegisterMessageCallback(nicknames.AddChatter);
    }

    public void Update()
    {
        if (lastTwitchChannel != PluginConfig.TwitchChannel.Value)
        {
            lastTwitchChannel = PluginConfig.TwitchChannel.Value;
            
            chat.Reset();
            bosses.Reset();
            nicknames.Reset();
        }

        chat.Update();

        if (PluginConfig.EnableBosses.Value)
        {
            bosses.Update();
        }

        if (PluginConfig.EnableNicknames.Value)
        {
            nicknames.Update();
        }
    }

    public void OnDestroy()
    {
        chat?.DisconnectFromTwitch();
        bosses?.Reset();
        nicknames?.Reset();
    }
}