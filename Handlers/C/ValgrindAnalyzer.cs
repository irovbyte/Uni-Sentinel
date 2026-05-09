using UniSentinel.Core;
namespace UniSentinel.Handlers.C;
internal static class ValgrindAnalyzer
{
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
                {
                    return (File.GetUnixFileMode(f) & UnixFileMode.UserExecute) != 0;
                }
                catch { return false; }
            }).ToList();
        if (binaries.Count == 0)
        {
            Logger.Info("Исполняемые файлы не найдены.");
            return true;
        }
        var allClean = true;
        foreach (var bin in binaries)
        {
            Logger.Info($"Анализ: {Path.GetFileName(bin)}...");
            var binDir = Path.GetDirectoryName(bin);
            var (_, _, err) = await runAsync("valgrind", $"--tool=memcheck --leak-check=full ./{Path.GetFileName(bin)}", binDir);
            if (err.Contains("ERROR SUMMARY: 0 errors"))
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
