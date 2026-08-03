using Spectre.Console;

namespace UniSentinel.Core;

internal static class Logger
{
    private static readonly Random t_rnd = new();
    private static readonly char[] t_noise = ['!', '@', '#', '$', '%', '^', '&', '*', '?', 'X', 'Z', '0', '1', '░', '▒', '▓'];

    public static void Header(string m)
    {
        var rule = new Rule($"[bold yellow]{m.ToUpperInvariant()}[/]")
        {
            Justification = Justify.Left
        };
        AnsiConsole.Write(rule);
    }

    public static void Success(string m) => AnsiConsole.MarkupLine($"[bold green][[OK]][/] {Markup.Escape(m)}");
    public static void Fail(string m) => AnsiConsole.MarkupLine($"[bold red][[ERR]][/] {Markup.Escape(m)}");
    public static void Warning(string m) => AnsiConsole.MarkupLine($"[bold yellow][[!]][/] {Markup.Escape(m)}");

    public static void Info(string m)
    {
        AnsiConsole.Markup($"[grey][[...]][/] ");
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
