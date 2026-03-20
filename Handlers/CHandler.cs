#pragma warning disable CA1416
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace UniSentinel.Handlers;

public class CHandler : BaseHandler
{
    private HashSet<string> _makeDirs = new();
    public CHandler(string p, List<string> f) : base(p, f) { }
    
    private async Task<(int Code, string Out, string Err)> RunAsync(string cmd, string args, string? customDir = null)
    {
        try
        {
            var info = new ProcessStartInfo(cmd, args)
            {
                WorkingDirectory = customDir ?? ProjectPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            using var p = Process.Start(info);
            if (p == null) return (1, "", "Failed to start");
            string o = await p.StandardOutput.ReadToEndAsync();
            string e = await p.StandardError.ReadToEndAsync();
            await p.WaitForExitAsync();
            return (p.ExitCode, o, e);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            Logger.Warning($"Инструмент '{cmd}' не найден в системе. Проверь PATH.");
            return (127, "", "");
        }
    }

    public override async Task<(bool Ok, int Points)> CheckGitAsync()
    {
        Logger.Header("ЭТАП 0: ИНСПЕКЦИЯ GIT");
        var res = await RunAsync("git", "rev-parse --abbrev-ref HEAD");
        if (res.Code != 0)
        {
            Logger.Warning("Git не инициализирован. Пропускаю проверку веток.");
            return (true, 0);
        }
        string branch = res.Out.Trim();
        if (branch == "master" || branch == "main")
        {
            Logger.Fail($"Обнаружена ветка '{branch}'! Правила требуют работать только в 'develop'.");
            return (false, 0);
        }
        Logger.Success($"Активная ветка: {branch}. Всё по правилам.");
        return (true, 0);
    }

    public override async Task<(bool Ok, int Points)> CheckStyleAsync()
    {
        Logger.Header("ЭТАП 1: СТИЛЬ (CLANG)");
        
        // 1. Создаем или копируем конфиг
        string? existingClang = Files.FirstOrDefault(f => Path.GetFileName(f) == ".clang-format");
        var cDirs = Files.Where(x => x.EndsWith(".c") || x.EndsWith(".h"))
                         .Select(Path.GetDirectoryName).Distinct().Where(d => d != null);
        
        foreach (var dir in cDirs)
        {
            string targetPath = Path.Combine(dir!, ".clang-format");
            if (existingClang != null && existingClang != targetPath) 
                File.Copy(existingClang, targetPath, true);
            else if (existingClang == null && !File.Exists(targetPath)) 
                await File.WriteAllTextAsync(targetPath, "BasedOnStyle: Google\n");
        }
        
        if (existingClang == null) Logger.Info("Сгенерирован стандартный Google Style (.clang-format).");

        var cFiles = Files.Where(x => x.EndsWith(".c") || x.EndsWith(".h")).ToList();
        var broken = new List<string>();

        // 2. Проверяем файлы (без жесткого Werror, используем --dry-run для безопасной проверки)
        foreach (var f in cFiles)
        {
            if (f.Contains("test")) continue; // Тесты часто имеют свой стиль
            var r = await RunAsync("clang-format", $"--dry-run -Werror {f}");
            if (r.Code != 0) broken.Add(f);
        }

        if (!broken.Any())
        {
            Logger.Success("Стиль идеален!");
            return (true, 0);
        }

        Logger.Fail($"Нарушен стиль в {broken.Count} файлах.");
        Console.Write("Исправить автоматически (y) или пропустить (s)? [y/s]: ");
        
        if (Console.ReadLine()?.ToLower() == "y")
        {
            // 3. Жесткое исправление всех C/H файлов на месте (-i)
            foreach (var f in cFiles)
            {
                if (!f.Contains("test")) await RunAsync("clang-format", $"-i -style=file {f}");
            }
            Logger.Success("Стиль успешно исправлен во всех файлах.");
            return (true, 0);
        }
        
        Logger.Warning("Проверка стиля проигнорирована. Идем дальше.");
        return (true, 0);
    }

    public override async Task<(bool Ok, int Points)> BuildAsync()
    {
        Logger.Header("ЭТАП 2: УМНАЯ СБОРКА (MULTI-MAKE)");
        var makefiles = Directory.GetFiles(ProjectPath, "Makefile", SearchOption.AllDirectories);
        if (!makefiles.Any())
        {
            Logger.Info("Makefile отсутствует. Этап сборки проигнорирован.");
            return (true, 0);
        }
        bool allOk = true;
        foreach (var make in makefiles)
        {
            string makeDir = Path.GetDirectoryName(make)!;
            _makeDirs.Add(makeDir);
            Logger.Info($"Анализ Makefile в: {Path.GetRelativePath(ProjectPath, makeDir)}");
            string content = await File.ReadAllTextAsync(make);
            
            var targets = Regex.Matches(content, @"^([a-zA-Z0-9_-]+):", RegexOptions.Multiline)
                               .Select(m => m.Groups[1].Value)
                               .Where(t => t != "clean" && t != "rebuild")
                               .Distinct().ToList();
            
            var phonyMatch = Regex.Match(content, @"^\.PHONY:\s*(.+)$", RegexOptions.Multiline);
            if (phonyMatch.Success)
            {
                var phonyTargets = phonyMatch.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var pt in phonyTargets)
                {
                    if (pt != "clean" && pt != "rebuild" && !targets.Contains(pt)) targets.Add(pt);
                }
            }
            
            var queue = new List<string>();
            if (targets.Contains("all")) queue.Add("all");
            else if (targets.Any()) queue.Add(targets.First());
            
            if (targets.Contains("test")) queue.Add("test");
            else if (targets.Contains("tests")) queue.Add("tests");
            
            if (targets.Contains("gcov_report")) queue.Add("gcov_report");
            
            Logger.Info($"Сформирована очередь выполнения: [{string.Join(" -> ", queue)}]");
            
            foreach (var target in queue)
            {
                Console.WriteLine($"   {Config.Settings.Colors.Gray}├─ Выполнение: make {target}...{Config.Settings.Colors.Reset}");
                var sw = Stopwatch.StartNew();
                var res = await RunAsync("make", target, makeDir);
                sw.Stop();
                
                if (res.Code != 0)
                {
                    Console.WriteLine($"   {Config.Settings.Colors.Fail}└─ [ERR] Ошибка при сборке '{target}'!{Config.Settings.Colors.Reset}");
                    var errLines = res.Err.Split('\n', StringSplitOptions.RemoveEmptyEntries).TakeLast(2);
                    foreach (var el in errLines) Console.WriteLine($"      {Config.Settings.Colors.Gray}{el.Trim()}{Config.Settings.Colors.Reset}");
                    allOk = false;
                    break;
                }
                else
                {
                    Console.WriteLine($"   {Config.Settings.Colors.Success}└─ [OK] Завершено ({sw.ElapsedMilliseconds} ms){Config.Settings.Colors.Reset}");
                }
            }
        }
        return (allOk, 0);
    }

    public override async Task<(bool Ok, int Points)> CheckMemoryAsync()
    {
        Logger.Header("ЭТАП 3: ПАМЯТЬ (VALGRIND)");
        var binaries = Directory.GetFiles(ProjectPath, "*", SearchOption.AllDirectories)
            .Where(f =>
            {
                if (f.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}")) return false;
                var name = Path.GetFileName(f);
                if (name.Contains(".") || new[] { "Makefile", "LICENSE", "CHANGELOG", "README" }.Contains(name, StringComparer.OrdinalIgnoreCase)) return false;
                try { return (File.GetUnixFileMode(f) & UnixFileMode.UserExecute) != 0; }
                catch { return false; }
            }).ToList();
            
        if (!binaries.Any())
        {
            Logger.Info("Бинарные файлы не найдены. Этап Valgrind проигнорирован.");
            return (true, 0);
        }
        
        bool allClean = true;
        foreach (var bin in binaries)
        {
            Logger.Info($"Анализ: {Path.GetFileName(bin)}...");
            string binDir = Path.GetDirectoryName(bin)!;
            var res = await RunAsync("valgrind", $"--tool=memcheck --leak-check=full ./{Path.GetFileName(bin)}", binDir);
            
            if (res.Err.Contains("ERROR SUMMARY: 0 errors"))
            {
                Logger.Success($"Память абсолютно чиста ({Path.GetFileName(bin)}).");
            }
            else
            {
                Logger.Fail($"Обнаружены утечки памяти в {Path.GetFileName(bin)}!");
                var summary = res.Err.Split('\n').Where(l => l.Contains("lost:") || l.Contains("ERROR SUMMARY:"));
                foreach (var line in summary) Console.WriteLine($"   \x1b[90m{line.Trim()}\x1b[0m");
                allClean = false;
            }
        }
        return (allClean, 0);
    }

    public override async Task<(bool Ok, int Points)> CheckCpuAsync()
    {
        Logger.Header("ЭТАП 4: ПРОФИЛИРОВАНИЕ CPU");
        var binaries = Directory.GetFiles(ProjectPath, "*", SearchOption.AllDirectories)
            .Where(f =>
            {
                if (f.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}")) return false;
                if (Path.GetFileName(f).Contains(".")) return false;
                try { return (File.GetUnixFileMode(f) & UnixFileMode.UserExecute) != 0; } catch { return false; }
            }).ToList();
            
        if (!binaries.Any())
        {
            Logger.Info("Исполняемые файлы отсутствуют. Замер скорости проигнорирован.");
            return (true, 0);
        }
        
        foreach (var bin in binaries)
        {
            string binDir = Path.GetDirectoryName(bin)!;
            Logger.Info($"Замер скорости: {Path.GetFileName(bin)}...");
            var sw = Stopwatch.StartNew();
            var res = await RunAsync(bin, "--help", binDir);
            sw.Stop();
            long ms = sw.ElapsedMilliseconds;
            string color = ms < 100 ? Config.Settings.Colors.Success : Config.Settings.Colors.Warning;
            Console.WriteLine($"   \x1b[90m-> Время выполнения CPU:\x1b[0m {color}{ms} мс\x1b[0m");
        }
        return (true, 0);
    }

    public override async Task<(bool Ok, int Points)> CheckAntiCheatAsync()
    {
        Logger.Header("ЭТАП 0.5: АНТИ-ЧИТ (СТРОГИЙ КОНТРОЛЬ)");
        var cFiles = Files.Where(x => x.EndsWith(".c")).ToList();
        
        // Массив запрещенных функций без скобок
        string[] banned = { "printf", "strcpy", "strcat", "strlen", "scanf" };
        bool allOk = true;
        
        foreach (var f in cFiles)
        {
            // Пропускаем unit-тесты, так как там разрешено использовать printf
            if (f.Contains("test")) continue;

            var lines = await File.ReadAllLinesAsync(f);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.TrimStart().StartsWith("//") || line.TrimStart().StartsWith("/*") || line.TrimStart().StartsWith("*")) continue;
                
                foreach (var ban in banned)
                {
                    // Регулярное выражение: ищем точное слово, за которым (сразу или через пробел) идет открывающая скобка
                    string pattern = $@"\b{ban}\s*\(";
                    if (Regex.IsMatch(line, pattern))
                    {
                        Logger.Fail($"[{Path.GetFileName(f)}:{i + 1}] Использование запрещенной функции: {ban}()");
                        allOk = false;
                    }
                }
            }
        }
        if (allOk) Logger.Success("Запрещенные вызовы не обнаружены. Код чист.");
        return (allOk, 0);
    }

    public override async Task<(bool Ok, int Points)> CheckStructureAsync()
    {
        Logger.Header("ЭТАП 5: СТРУКТУРНОЕ ПРОГРАММИРОВАНИЕ");
        var cFiles = Files.Where(x => x.EndsWith(".c") || x.EndsWith(".h")).ToList();
        if (!cFiles.Any()) return (true, 0);
        bool allOk = true;
        foreach (var f in cFiles)
        {
            var lines = await File.ReadAllLinesAsync(f);
            int blockDepth = 0;
            int functionLines = 0;
            bool inFunction = false;
            bool depthErrorShown = false;
            
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (Regex.IsMatch(line, @"\bgoto\b"))
                {
                    Logger.Fail($"[{Path.GetFileName(f)}:{i + 1}] Обнаружен 'goto'!");
                    allOk = false;
                }
                
                if (line.Contains("{"))
                {
                    blockDepth++;
                    if (blockDepth == 1) { inFunction = true; functionLines = 0; }
                }
                
                if (inFunction) functionLines++;
                
                if (line.Contains("}"))
                {
                    blockDepth--;
                    if (blockDepth == 0 && inFunction)
                    {
                        if (functionLines > 50)
                        {
                            Logger.Fail($"[{Path.GetFileName(f)}] Размер функции превышает 50 строк (Текущий: {functionLines})! Декомпозируй.");
                            allOk = false;
                        }
                        inFunction = false;
                    }
                }
                
                if (blockDepth > 4 && !depthErrorShown)
                {
                    Logger.Fail($"[{Path.GetFileName(f)}:{i + 1}] Вложенность блоков > 4");
                    allOk = false;
                    depthErrorShown = true;
                }
            }
        }
        if (allOk) Logger.Success("Структура кода соответствует стандартам Дейкстры (функции < 50 строк).");
        return (allOk, 0);
    }

    public override async Task<bool> StripCommentsAsync()
    {
        Logger.Header("ЭТАП 6: ОЧИСТКА КОДА ОТ КОММЕНТАРИЕВ");
        var cFiles = Files.Where(x => x.EndsWith(".c") || x.EndsWith(".h")).ToList();
        if (!cFiles.Any()) return true;
        
        string pattern = @"(@(?:""[^""]*"")+|""(?:[^""\n\\]+|\\.)*""|'(?:[^'\n\\]+|\\.)*')|//.*|/\*[\s\S]*?\*/";
        var filesToClean = new Dictionary<string, string>();
        
        foreach (var f in cFiles)
        {
            string text = await File.ReadAllTextAsync(f);
            string cleanText = Regex.Replace(text, pattern, m =>
            {
                if (m.Groups[1].Success) return m.Value;
                return "";
            });
            cleanText = Regex.Replace(cleanText, @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);
            if (text != cleanText)
            {
                filesToClean[f] = cleanText;
            }
        }
        
        if (!filesToClean.Any())
        {
            Logger.Info("Комментарии в коде отсутствуют. Этап пройден автоматически.");
            return true;
        }
        
        Console.Write($" {Config.Settings.Colors.LycorisAccent}Найдено комментариев в {filesToClean.Count} файлах. Вырезать весь мусор перед сдачей? [y/N]: {Config.Settings.Colors.Reset}");
        if (Console.ReadLine()?.Trim().ToLower() != "y")
        {
            Logger.Info("Очистка комментариев проигнорирована.");
            return true;
        }
        
        foreach (var kvp in filesToClean)
        {
            await File.WriteAllTextAsync(kvp.Key, kvp.Value);
        }
        Logger.Success($"Комментарии успешно вырезаны из {filesToClean.Count} файлов.");
        return true;
    }

    public override async Task<bool> CleanupAsync()
    {
        Logger.Header("ФИНАЛ: ОЧИСТКА");
        if (!_makeDirs.Any())
        {
            Logger.Info("Нечего очищать. Папки чисты.");
            return true;
        }
        foreach (var dir in _makeDirs)
        {
            await RunAsync("make", "clean", dir);
            Logger.Info($"Убрано за собой в папке: {Path.GetRelativePath(ProjectPath, dir)}");
        }
        Logger.Success("Все временные файлы успешно удалены.");
        return true;
    }
}