using MultiBloxy.Core;

namespace MultiBloxy;

internal static class AppConstants
{
    public const string Name = "MultiBloxy";
    public const string RobloxGuardName = "ROBLOX_singletonEvent";
    public const string RobloxProcessName = "RobloxPlayerBeta";
    public const string Homepage = "https://github.com/Zgoly/MultiBloxy";

    public static string DataDirectory =>
        Path.GetDirectoryName(SettingsStore.GetDefaultPath(Name))
        ?? AppContext.BaseDirectory;

    public static string SettingsPath => SettingsStore.GetDefaultPath(Name);

    public static string LegacySettingsPath =>
        Path.Combine(AppContext.BaseDirectory, "config.xml");

    public static string LogDirectory => Path.Combine(DataDirectory, "logs");
}
