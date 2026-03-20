namespace UniSentinel.Core;

public static class SettingsManager
{
    private static readonly string ConfigFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".uni-sentinel-config"
    );

    public static bool IsAntiCheatEnabled()
    {
        if (!File.Exists(ConfigFile)) return false;
        return File.ReadAllText(ConfigFile).Trim() == "AC=1";
    }

    public static void SetAntiCheat(bool enable)
    {
        File.WriteAllText(ConfigFile, enable ? "AC=1" : "AC=0");
        if (enable) Logger.Success("Режим Anti-Cheat ВКЛЮЧЕН. Пощады не будет.");
        else Logger.Info("Режим Anti-Cheat ВЫКЛЮЧЕН.");
    }
}