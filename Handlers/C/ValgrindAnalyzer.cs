using System.Text.RegularExpressions;
using UniSentinel.Core;
namespace UniSentinel.Handlers.C;

internal static partial class ValgrindAnalyzer
{
    [GeneratedRegex(@"ERROR SUMMARY: [1-9]\d* errors")]
    private static partial Regex ErrorSummaryRegex();
    public static async Task<bool> CheckAsync(string rootPath, Func<string, string, string?, Task<(int Code, string Out, string Err)>> runAsync)
    {
        Logger.Header("ЭТАП 3: АНАЛИЗ ПАМЯТИ (VALGRIND)");
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Logger.Info("Valgrind доступен только в Linux. Пропуск.");
            return true;
        }
        var binaries = Directory.GetFiles(rootPath, "*", SearchOption.AllDirectories)
            .Where(f => !Path.GetFileName(f).Contains('.') && !f.Contains(".git") && !f.Contains(".uni-cache"))
            .Where(f =>
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return false;
                }

                try
                { return (File.GetUnixFileMode(f) & UnixFileMode.UserExecute) != 0; }
                catch { return false; }
            }).ToList();

        if (binaries.Count == 0)
        {
            var makefiles = Directory.GetFiles(rootPath, "Makefile", SearchOption.AllDirectories);
            if (makefiles.Length > 0)
            {
                Logger.Warning("Бинарники удалены (вероятно после gcov_report). Пересобираем тесты...");
                foreach (var make in makefiles)
                {
                    _ = await runAsync("make", "test", Path.GetDirectoryName(make));
                }
                binaries = [.. Directory.GetFiles(rootPath, "*", SearchOption.AllDirectories)
                    .Where(f => !Path.GetFileName(f).Contains('.') && !f.Contains(".git") && !f.Contains(".uni-cache"))
                    .Where(f =>
                    {
                        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                        {
                            return false;
                        }

                        try
                        { return (File.GetUnixFileMode(f) & UnixFileMode.UserExecute) != 0; }
                        catch { return false; }
                    })];
            }
        }

        if (binaries.Count == 0)
        {
            Logger.Info("Исполняемые файлы не найдены даже после пересборки.");
            return true;
        }
        var allClean = true;
        foreach (var bin in binaries)
        {
            Logger.Info($"Анализ: {Path.GetFileName(bin)}...");
            var binDir = Path.GetDirectoryName(bin);
            var (_, _, err) = await runAsync("valgrind", $"--tool=memcheck --leak-check=full ./{Path.GetFileName(bin)}", binDir);

            var hasErrors = err.Contains("definitely lost:") && !err.Contains("definitely lost: 0 bytes");
            if (ErrorSummaryRegex().IsMatch(err))
            {
                hasErrors = true;
            }

            if (!hasErrors)
            {
                Logger.Success($"Память абсолютно чиста ({Path.GetFileName(bin)}).");
            }
            else
            {
                Logger.Fail($"Обнаружены утечки или ошибки доступа в {Path.GetFileName(bin)}!");
                var summary = err.Split('\n').Where(l =>
                    l.Contains("lost:") || l.Contains("ERROR SUMMARY:") ||
                    l.Contains("uninitialized") || l.Contains("Invalid"));
                foreach (var line in summary)
                {
                    Console.WriteLine($"   \x1b[90m{line.Trim()}\x1b[0m");
                }
                allClean = false;
            }
        }
        return allClean;
    }
}
