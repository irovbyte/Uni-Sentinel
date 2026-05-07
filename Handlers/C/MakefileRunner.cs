using System.Text.RegularExpressions;
using System.Diagnostics;
using UniSentinel.Core;
namespace UniSentinel.Handlers.C;
internal static partial class MakefileRunner
{
    private static readonly string[] t_standardTargets = ["all", "test", "gcov_report"];
    [GeneratedRegex(@"^([a-zA-Z0-9_-]+):", RegexOptions.Multiline)]
    private static partial Regex TargetRegex();
    public static async Task<(bool Ok, List<string> Dirs)> RunSequenceAsync(string rootPath)
    {
        Logger.Header("ЭТАП 2: УМНАЯ СБОРКА ПРОЕКТА");
        var makefiles = Directory.GetFiles(rootPath, "Makefile", SearchOption.AllDirectories);
        var activeDirs = new List<string>();
        if (makefiles.Length == 0)
        {
            Logger.Info("Makefile отсутствует. Сборка пропущена.");
            return (true, activeDirs);
        }
        var allOk = true;
        foreach (var make in makefiles)
        {
            var dir = Path.GetDirectoryName(make)!;
            activeDirs.Add(dir);
            var content = await File.ReadAllTextAsync(make);
            var targets = TargetRegex().Matches(content)
                 .Select(m => m.Groups[1].Value)
                 .ToHashSet();
            var queue = t_standardTargets.Where(targets.Contains);
            foreach (var target in queue)
            {
                Console.WriteLine($"   {Settings.Colors.Gray}├─ Выполнение: make {target}...{Settings.Colors.Reset}");
                var startTimestamp = Stopwatch.GetTimestamp();
                var info = new ProcessStartInfo("make", target)
                {
                    WorkingDirectory = dir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(info);
                if (p is null)
                {
                    Logger.Fail($"Не удалось запустить 'make' в {dir}");
                    allOk = false;
                    break;
                }
                var err = await p.StandardError.ReadToEndAsync();
                await p.WaitForExitAsync();
                var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
                if (p.ExitCode != 0)
                {
                    Console.WriteLine($"   {Settings.Colors.Fail}└─ [ERR] Ошибка при сборке '{target}'!{Settings.Colors.Reset}");
                    var errorLines = err.Split('\n', StringSplitOptions.RemoveEmptyEntries).TakeLast(3);
                    foreach (var el in errorLines)
                    {
                        Console.WriteLine($"      {Settings.Colors.Gray}{el.Trim()}{Settings.Colors.Reset}");
                    }
                    allOk = false;
                    break;
                }
                Console.WriteLine($"   {Settings.Colors.Success}└─ [OK] Завершено ({elapsed.TotalMilliseconds:F0} ms){Settings.Colors.Reset}");
            }
        }
        return (allOk, activeDirs);
    }
}
