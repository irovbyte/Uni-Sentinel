namespace UniSentinel.Core;
internal static class SettingsManager
{
    private static readonly string t_configFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".uni_config",
        "anti-cheat.cfg"
    );
    public static bool IsAntiCheatEnabled() =>
        File.Exists(t_configFile) && File.ReadAllText(t_configFile).Trim() is "AC=1";
    public static void SetAntiCheat(bool enable)
    {
        _ = Directory.CreateDirectory(Path.GetDirectoryName(t_configFile)!);
        File.WriteAllText(t_configFile, enable ? "AC=1" : "AC=0");
        if (enable)
        {
            Logger.Success("Режим Anti-Cheat ВКЛЮЧЕН. Пощады не будет.");
        }
        else
        {
            Logger.Info("Режим Anti-Cheat ВЫКЛЮЧЕН.");
        }
    }
}
