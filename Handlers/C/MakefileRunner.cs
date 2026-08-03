namespace UniSentinel.Handlers.C;

internal static partial class MakefileRunner
{
    [GeneratedRegex(@"^([a-zA-Z0-9_-]+):", RegexOptions.Multiline)]
    private static partial Regex TargetRegex();

    [GeneratedRegex(@"^\s*(-?rm\s+.*?)$", RegexOptions.Multiline)]
    private static partial Regex RmCommandRegex();
    public static async Task<(bool Ok, List<string> Dirs)> RunSequenceAsync(string rootPath)
    {
        Logger.Header("ЭТАП 2: УМНАЯ СБОРКА ПРОЕКТА");
        var makefiles = Directory.GetFiles(rootPath, "Makefile", SearchOption.AllDirectories);
        var activeDirs = new List<string>();
        if (makefiles.Length == 0)
        {
            Logger.Warning("Makefile отсутствует! Запуск Piscine Mode (авто-компиляция)...");
            var cFiles = Directory.GetFiles(rootPath, "*.c", SearchOption.AllDirectories)
                .Where(f => !f.Contains("test") && !f.Contains("check"))
                .ToList();
            if (cFiles.Count > 0)
            {
                Console.WriteLine($"   {Settings.Colors.Gray}├─ Выполнение: gcc -Wall -Werror -Wextra *.c...{Settings.Colors.Reset}");
                var startTimestamp = Stopwatch.GetTimestamp();
                var info = new ProcessStartInfo("gcc", $"-Wall -Werror -Wextra {string.Join(" ", cFiles.Select(c => $"\"{c}\""))} -o uni_piscine_out")
                {
                    WorkingDirectory = rootPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(info);
                if (p != null)
                {
                    var err = await p.StandardError.ReadToEndAsync();
                    await p.WaitForExitAsync();
                    var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
                    if (p.ExitCode != 0)
                    {
                        Console.WriteLine($"   {Settings.Colors.Fail}└─ [ERR] Ошибка при авто-компиляции!{Settings.Colors.Reset}");
                        var errorLines = err.Split('\n', StringSplitOptions.RemoveEmptyEntries).TakeLast(3);
                        foreach (var el in errorLines)
                        {
                            Console.WriteLine($"      {Settings.Colors.Gray}{el.Trim()}{Settings.Colors.Reset}");
                        }
                        return (false, activeDirs);
                    }
                    Console.WriteLine($"   {Settings.Colors.Success}└─ [OK] Завершено ({elapsed.TotalMilliseconds:F0} ms){Settings.Colors.Reset}");
                }
            }
            else
            {
                Logger.Info("Нет .c файлов для компиляции.");
            }
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

            var queue = new List<string>();
            if (targets.Contains("all"))
            {
                queue.Add("all");
            }
            else
            {
                var lib = targets.FirstOrDefault(t => t.EndsWith(".a"));
                if (lib != null)
                {
                    queue.Add(lib);
                }
            }

            var testTargets = targets.Where(t => t.StartsWith("test") || t.StartsWith("check")).ToList();
            if (testTargets.Count > 0)
            {
                queue.AddRange(testTargets);
            }

            var covTargets = targets.Where(t => t.StartsWith("gcov") || t.StartsWith("coverage")).ToList();
            if (covTargets.Count > 0)
            {
                queue.AddRange(covTargets);
            }

            // Create Shadow Makefile to prevent deletion during Phase 2
            var shadowContent = RmCommandRegex().Replace(content, "\t@echo \"[Shadow] rm blocked: $$1\"");
            var shadowFile = Path.Combine(dir, "Makefile.shadow");
            await File.WriteAllTextAsync(shadowFile, shadowContent);

            foreach (var target in queue)
            {
                Console.WriteLine($"   {Settings.Colors.Gray}├─ Выполнение: make {target}...{Settings.Colors.Reset}");
                var startTimestamp = Stopwatch.GetTimestamp();
                var info = new ProcessStartInfo("make", $"-f Makefile.shadow {target}")
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
            if (File.Exists(shadowFile))
            {
                File.Delete(shadowFile);
            }
        }
        return (allOk, activeDirs);
    }
}
