using System.Globalization;
using UniSentinel.Core;
namespace UniSentinel.Core;
internal static class ScoreManager
{
    private static readonly string t_configDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".uni_config");
    private static readonly string t_scoreFile = Path.Combine(t_configDir, "score.txt");
    private static readonly string t_streakFile = Path.Combine(t_configDir, "streak.txt");
    public static int LoadScore() =>
        Directory.CreateDirectory(t_configDir) is not null && File.Exists(t_scoreFile) &&
        int.TryParse(File.ReadAllText(t_scoreFile), out var s) ? s : 0;
    public static void AddPoints(int points) =>
        File.WriteAllText(t_scoreFile, (LoadScore() + points).ToString(CultureInfo.InvariantCulture));
    public static void UpdateStreak()
    {
        _ = Directory.CreateDirectory(t_configDir);
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var lastDateStr = File.Exists(t_streakFile) ? File.ReadAllText(t_streakFile) : "";
        if (lastDateStr == today)
        {
            return;
        }
        if (DateTime.TryParse(lastDateStr, out var lastDate))
        {
            var diff = (DateTime.Now.Date - lastDate.Date).Days;
            if (diff == 1)
            {
                Logger.Warning("STREAK! Ты в огне! Множитель XP x1.5 активен.");
            }
            else if (diff > 1)
            {
                Logger.Info("Стрик потерян. Пора возвращаться в строй, Shadow Monarch.");
            }
        }
        File.WriteAllText(t_streakFile, today);
    }
    public static (string RankName, string MainColor, string AccentColor, string Prefix, string Suffix) GetRankInfo()
    {
        var s = LoadScore();
        return s switch
        {
            < 1 => ("Trainee", Settings.Colors.TraineeMain, Settings.Colors.TraineeAccent, ">>", "<<"),
            < 5 => ("Awakened", Settings.Colors.AwakeMain, Settings.Colors.AwakeAccent, "~", "~"),
            < 10 => ("Sentinel", Settings.Colors.SentinelMain, Settings.Colors.SentinelAccent, "==", "=="),
            < 15 => ("Despair Scholar", Settings.Colors.DespairMain, Settings.Colors.DespairAccent, "×", "×"),
            < 25 => ("Cyber Runner", Settings.Colors.CyberMain, Settings.Colors.CyberAccent, "/>", "</"),
            < 40 => ("Lycoris Elite", Settings.Colors.LycorisMain, Settings.Colors.LycorisAccent, "✧", "✧"),
            < 60 => ("Opium Initiate", Settings.Colors.OpiumMain, Settings.Colors.OpiumAccent, "‡", "‡"),
            < 80 => ("The Void", Settings.Colors.VoidMain, Settings.Colors.VoidAccent, "||", "||"),
            < 100 => ("VIPER BOSS", Settings.Colors.ViperMain, Settings.Colors.ViperAccent, "†", "†"),
            < 150 => ("SHADOW MONARCH", Settings.Colors.MonarchMain, Settings.Colors.MonarchAccent, "★", "★"),
            < 250 => ("CODE SOVEREIGN", Settings.Colors.SovereignMain, Settings.Colors.SovereignAccent, "♛", "♛"),
            < 400 => ("NEON ARCHITECT", Settings.Colors.NeonMain, Settings.Colors.NeonAccent, "∆", "∆"),
            < 600 => ("QUANTUM GHOST", Settings.Colors.GhostMain, Settings.Colors.GhostAccent, "◊", "◊"),
            < 850 => ("MATRIX DEITY", Settings.Colors.MatrixMain, Settings.Colors.MatrixAccent, "Ω", "Ω"),
            _ => ("CORE SINGULARITY", Settings.Colors.SingularityMain, Settings.Colors.SingularityAccent, "∞", "∞")
        };
    }
    public static void PrintRankBanner()
    {
        var (rankName, mainColor, accentColor, _, _) = GetRankInfo();
        var score = LoadScore();
        var (top, mid, bot) = score switch
        {
            < 25 => ("┌────────────────────────────────────────┐", "│", "└────────────────────────────────────────┘"),
            < 100 => ("╔════════════════════════════════════════╗", "║", "╚════════════════════════════════════════╝"),
            < 400 => ("▛▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▜", "▌", "▙▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▟"),
            _ => ("██████████████████████████████████████████", "██", "██████████████████████████████████████████")
        };
        Console.WriteLine($"{accentColor}{top}");
        Console.WriteLine($"{mid}  ТВОЙ РАНГ: {mainColor}{rankName}{accentColor,-20}{mid}");
        Console.WriteLine($"{mid}  ОЧКИ XP:   {score,-28} {mid}");
        Console.WriteLine($"{bot}{Settings.Colors.Reset}\n");
    }
}
