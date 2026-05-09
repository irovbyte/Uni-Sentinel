using System.Text.RegularExpressions;
using UniSentinel.Core;
namespace UniSentinel.Handlers.C;
internal static partial class CoverageAnalyzer
{
    [GeneratedRegex(@"^\s*([#=]+|-|\d+):\s*(\d+):(.*)$")]
    private static partial Regex GcovLineRegex();
    public static async Task<bool> AnalyzeAsync(string rootPath, Func<string, string, string?, Task<(int Code, string Out, string Err)>> runAsync)
    {
        Logger.Header("ЭТАП 4: АНАЛИЗ ПОКРЫТИЯ (SHADOW GCOV)");
        if (Directory.GetFiles(rootPath, "Makefile", SearchOption.AllDirectories).Length == 0)
        {
            return true;
        }
        var cacheDir = Path.Combine(rootPath, ".uni-cache", "gcov");
        _ = Directory.CreateDirectory(cacheDir);
        var gcnoFiles = Directory.GetFiles(rootPath, "*.gcno", SearchOption.AllDirectories);
        foreach (var file in gcnoFiles)
        {
            _ = await runAsync("gcov", $"\"{file}\"", cacheDir);
        }
        var gcovFiles = Directory.GetFiles(cacheDir, "*.gcov");
        var allCovered = true;
        foreach (var file in gcovFiles)
        {
            var lines = await File.ReadAllLinesAsync(file);
            var uncovered = lines
                .Select(line => GcovLineRegex().Match(line))
                .Where(m => m.Success && (m.Groups[1].Value.Contains('#') || m.Groups[1].Value.Contains('=')))
                .Select(m =>
                {
                    var type = m.Groups[1].Value.Contains('#') ? "[LINE]" : "[BRCH]";
                    return $"  {Settings.Colors.Warning}{type} {m.Groups[2].Value,4} |{Settings.Colors.Reset} {m.Groups[3].Value.Trim()}";
                })
                .ToList();
            if (uncovered.Count > 0)
            {
                allCovered = false;
                Console.WriteLine($"\n {Settings.Colors.Fail}[GCOV]{Settings.Colors.Reset} Файл: {Settings.Colors.LycorisAccent}{Path.GetFileNameWithoutExtension(file)}{Settings.Colors.Reset}");
                foreach (var line in uncovered)
                {
                    Console.WriteLine(line);
                }
            }
        }
        if (allCovered && gcovFiles.Length > 0)
        {
            Logger.Success("Покрытие: 100%. Код полностью протестирован.");
        }
        return allCovered;
    }
}
