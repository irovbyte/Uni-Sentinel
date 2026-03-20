namespace UniSentinel.Core;

public static class Logger
{
    public static void Header(string m)
    {
        var rank = ScoreManager.GetRankInfo();
        Console.WriteLine($"\n{Settings.Colors.Bold}{rank.AccentColor}{rank.Prefix} {rank.MainColor}{m.ToUpper()} {rank.AccentColor}{rank.Suffix}{Settings.Colors.Reset}");
    }

    public static void Success(string m) => Console.WriteLine($" {Settings.Colors.Success}[OK]{Settings.Colors.Reset} {m}");
    public static void Fail(string m) => Console.WriteLine($" {Settings.Colors.Fail}[ERR]{Settings.Colors.Reset} {m}");
    public static void Warning(string m) => Console.WriteLine($" {Settings.Colors.Warning}[!]{Settings.Colors.Reset} {m}");
    public static void Info(string m) => Console.WriteLine($" {Settings.Colors.Gray}[...]{Settings.Colors.Reset} {m}");
}