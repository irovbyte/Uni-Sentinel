namespace UniSentinel.Core;
internal static class Logger
{
    private static readonly Random t_rnd = new();
    private static readonly char[] t_noise = ['!', '@', '#', '$', '%', '^', '&', '*', '?', 'X', 'Z', '0', '1', '░', '▒', '▓'];
    public static void Header(string m)
    {
        var (_, mainColor, accentColor, prefix, suffix) = ScoreManager.GetRankInfo();
        Console.WriteLine($"\n{Settings.Colors.Bold}{accentColor}{prefix} {mainColor}{m.ToUpperInvariant()} {accentColor}{suffix}{Settings.Colors.Reset}");
    }
    public static void Success(string m) => Console.WriteLine($" {Settings.Colors.Success}[OK]{Settings.Colors.Reset} {m}");
    public static void Fail(string m) => Console.WriteLine($" {Settings.Colors.Fail}[ERR]{Settings.Colors.Reset} {m}");
    public static void Warning(string m) => Console.WriteLine($" {Settings.Colors.Warning}[!]{Settings.Colors.Reset} {m}");
    public static void Info(string m)
    {
        Console.Write($" {Settings.Colors.Gray}[...]{Settings.Colors.Reset} ");
        TypeEffect(m);
        Console.WriteLine();
    }
    private static void TypeEffect(string text)
    {
        foreach (var c in text)
        {
            Console.Write(t_noise[t_rnd.Next(t_noise.Length)]);
            Thread.Sleep(3);
            Console.Write("\b");
            Console.Write(c);
            Thread.Sleep(2);
        }
    }
}
