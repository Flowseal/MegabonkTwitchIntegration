using UnityEngine;

namespace TwitchIntegration;

public class TwitchIntegrationBase : MonoBehaviour
{
    private readonly Chat chat = new Chat();
    private readonly Bosses bosses = new Bosses();
    private readonly Nicknames nicknames = new Nicknames();

    public void Awake()
    {
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;
        Plugin.Log.LogInfo("Awake called");

        chat.Reset();
        bosses.Reset();
        nicknames.Reset();
    }

    public void Start()
    {
        Plugin.Log.LogInfo("Start called");

        chat.RegisterMessageCallback(bosses.ProcessNewChatMessage);
        chat.RegisterMessageCallback(nicknames.AddChatter);

        PluginConfig.ConfigFile.SettingChanged += (sender, args) =>
        {
            chat.Reset();
            bosses.Reset();
            nicknames.Reset();
        };
    }

    public void Update()
    {
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