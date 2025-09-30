using BepInEx.Configuration;

namespace TwitchIntegration;

public static class PluginConfig
{
    public static ConfigEntry<string> TwitchChannel { get; private set; }
    
    public static ConfigEntry<bool> EnableBosses { get; private set; }
    public static ConfigEntry<float> BossesDialogSize { get; private set; }
    public static ConfigEntry<int> BossesMaxChatters { get; private set; }
    
    public static ConfigEntry<bool> EnableNicknames { get; private set; }
    public static ConfigEntry<float> NicknamesSize { get; private set; }
    public static ConfigEntry<int> NicknamesMaxChatters { get; private set; }

    public static ConfigFile ConfigFile { get; private set; }

    public static void Initialize(ConfigFile config)
    {
        // Base section
        TwitchChannel = config.Bind(
            "Base",
            "TwitchChannel",
            "flowseal",
            "The Twitch channel to connect to"
        );

        // Bosses section
        EnableBosses = config.Bind(
            "Bosses",
            "EnableBosses",
            true,
            "Enable assigning random chatter to the boss"
        );

        BossesDialogSize = config.Bind(
            "Bosses",
            "TextSize",
            28f,
            "Size of the boss text"
        );

        BossesMaxChatters = config.Bind(
            "Bosses",
            "MaxChatters",
            1000,
            "Maximum number of last chatters buffer (overwrites oldest users when full)"
        );

        // Nicknames section
        EnableNicknames = config.Bind(
            "Nicknames",
            "EnableNicknames",
            true,
            "Enable showing random chatter nicknames above creeps"
        );

        NicknamesSize = config.Bind(
            "Nicknames",
            "NicknameSize",
            12f,
            "Size of the nickname text for creeps"
        );

        NicknamesMaxChatters = config.Bind(
            "Nicknames",
            "MaxChatters",
            1000,
            "Maximum number of last chatters buffer (overwrites oldest users when full)"
        );
        
        ConfigFile = config;
    }
}