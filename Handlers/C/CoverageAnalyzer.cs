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
        if (gcnoFiles.Length == 0)
        {
            if (Directory.Exists(Path.Combine(rootPath, "report")) || Directory.GetFiles(rootPath, "*.info", SearchOption.AllDirectories).Length > 0)
            {
                Logger.Success("Исходные .gcno удалены (вероятно Makefile'ом), но найден готовый HTML/LCOV отчет!");
                return true;
            }
            Logger.Info("Файлы покрытия (.gcno) не найдены. Если Makefile их удаляет, проверь HTML-отчет вручную.");
            return true;
        }
        foreach (var file in gcnoFiles)
        {
            var dir = Path.GetDirectoryName(file);
            _ = await runAsync("gcov", $"\"{Path.GetFileName(file)}\"", dir);

            var generatedGcovs = Directory.GetFiles(dir!, "*.gcov");
            foreach (var gcovFile in generatedGcovs)
            {
                var dest = Path.Combine(cacheDir, Path.GetFileName(gcovFile));
                if (File.Exists(dest))
                {
                    File.Delete(dest);
                }

                File.Move(gcovFile, dest);
            }
        }
        var gcovFiles = Directory.GetFiles(cacheDir, "*.gcov");
        var allCovered = true;
        foreach (var file in gcovFiles)
        {
            var lines = await File.ReadAllLinesAsync(file);
            int totalExecutable = 0;
            int executed = 0;
            var uncovered = new List<string>();

            foreach (var line in lines)
            {
                var m = GcovLineRegex().Match(line);
                if (!m.Success) continue;
                
                var countStr = m.Groups[1].Value.Trim();
                if (countStr == "-") continue;
                
                totalExecutable++;
                if (countStr == "#####" || countStr.Contains("====="))
                {
                    var type = countStr.Contains('#') ? "[LINE]" : "[BRCH]";
                    uncovered.Add($"  {Settings.Colors.Warning}{type} {m.Groups[2].Value,4} |{Settings.Colors.Reset} {m.Groups[3].Value.Trim()}");
                }
                else
                {
                    executed++;
                }
            }
            
            double percent = totalExecutable == 0 ? 100.0 : (executed * 100.0 / totalExecutable);
            var percentStr = $"{percent:0.00}%";
            var color = percent == 100.0 ? Settings.Colors.Success : (percent >= 80.0 ? Settings.Colors.Warning : Settings.Colors.Fail);

            if (uncovered.Count > 0)
            {
                allCovered = false;
                Console.WriteLine($"\n {color}[{percentStr,7}]{Settings.Colors.Reset} Файл: {Settings.Colors.LycorisAccent}{Path.GetFileNameWithoutExtension(file)}{Settings.Colors.Reset}");
                foreach (var uLine in uncovered)
                {
                    Console.WriteLine(uLine);
                }
            }
            else
            {
                Console.WriteLine($" {color}[{percentStr,7}]{Settings.Colors.Reset} Файл: {Path.GetFileNameWithoutExtension(file)}");
            }
        }
        if (allCovered && gcovFiles.Length > 0)
        {
            Logger.Success("Покрытие: 100%. Код полностью протестирован.");
        }
        return allCovered;
    }
}
