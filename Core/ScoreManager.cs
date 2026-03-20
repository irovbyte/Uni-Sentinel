namespace UniSentinel.Core;

public static class ScoreManager
{
    private static readonly string ScoreFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".uni-sentinel-score"
    );
    public static int LoadScore()
    {
        if (File.Exists(ScoreFile) && int.TryParse(File.ReadAllText(ScoreFile), out int s)) return s;
        return 0;
    }
    public static void AddPoints(int points) => File.WriteAllText(ScoreFile, (LoadScore() + points).ToString());
    public static (string RankName, string MainColor, string AccentColor, string Prefix, string Suffix) GetRankInfo()
    {
        int score = LoadScore();
        if (score < 1) return ("Trainee", Settings.Colors.TraineeMain, Settings.Colors.TraineeAccent, ">>", "<<");
        if (score < 5) return ("Awakened", Settings.Colors.AwakeMain, Settings.Colors.AwakeAccent, "~", "~");
        if (score < 10) return ("Sentinel", Settings.Colors.SentinelMain, Settings.Colors.SentinelAccent, "==", "==");
        if (score < 15) return ("Despair Scholar", Settings.Colors.DespairMain, Settings.Colors.DespairAccent, "×", "×");
        if (score < 25) return ("Cyber Runner", Settings.Colors.CyberMain, Settings.Colors.CyberAccent, "/>", "</");
        if (score < 40) return ("Lycoris Elite", Settings.Colors.LycorisMain, Settings.Colors.LycorisAccent, "✧", "✧");
        if (score < 60) return ("Opium Initiate", Settings.Colors.OpiumMain, Settings.Colors.OpiumAccent, "‡", "‡");
        if (score < 80) return ("The Void", Settings.Colors.VoidMain, Settings.Colors.VoidAccent, "||", "||");
        if (score < 100) return ("VIPER BOSS", Settings.Colors.ViperMain, Settings.Colors.ViperAccent, "†", "†");
        return ("SHADOW MONARCH", Settings.Colors.MonarchMain, Settings.Colors.MonarchAccent, "★", "★");
    }
    public static void PrintRankBanner()
    {
        var rank = GetRankInfo();
        Console.WriteLine($"\n{rank.AccentColor}╔════════════════════════════════════════╗");
        Console.WriteLine($"║  ТВОЙ РАНГ: {rank.MainColor}{rank.RankName,-26}{rank.AccentColor} ║");
        Console.WriteLine($"║  ОЧКИ: {LoadScore(),-31} ║");
        Console.WriteLine($"╚════════════════════════════════════════╝{Settings.Colors.Reset}\n");
    }
}