using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UniSentinel.Core;
namespace UniSentinel.Handlers.C;
internal sealed partial class CHandler(string projectPath, List<string> files) : BaseHandler(projectPath, files)
{
    [GeneratedRegex(@"\b(printf|strcpy|strcat|strlen|scanf)\s*\(", RegexOptions.Compiled)]
    private static partial Regex BannedFunctionsRegex();
    [GeneratedRegex(@"\bgoto\b", RegexOptions.Compiled)]
    private static partial Regex GotoRegex();
    [GeneratedRegex(@"(@(?:""[^""]*"")+|""(?:[^""\n\\]+|\\.)*""|'(?:[^'\n\\]+|\\.)*')|//.*|/\*[\s\S]*?\*/", RegexOptions.Compiled)]
    private static partial Regex CommentsRegex();
    [GeneratedRegex(@"^\s+$[\r\n]*", RegexOptions.Multiline)]
    private static partial Regex EmptyLinesRegex();
    private List<string> _makeDirs = [];
    private async Task<(int Code, string Out, string Err)> RunAsync(string cmd, string args, string? customDir = null)
    {
        try
        {
            var info = new ProcessStartInfo(cmd, args)
            {
                WorkingDirectory = customDir ?? ProjectPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(info);
            if (p is null)
            {
                return (1, "", "Failed to start");
            }
            var o = await p.StandardOutput.ReadToEndAsync();
            var e = await p.StandardError.ReadToEndAsync();
            await p.WaitForExitAsync();
            return (p.ExitCode, o, e);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            Logger.Warning($"Инструмент '{cmd}' не найден. Проверь PATH.");
            return (127, "", "");
        }
    }
    public override async Task<bool> CheckDependenciesAsync() => await DependencyManager.RequireStackAsync("c");
    public override async Task<(bool Ok, int Points)> CheckGitAsync()
    {
        Logger.Header("ЭТАП 0: ИНСПЕКЦИЯ GIT");
        if (await RunAsync("git", "rev-parse --abbrev-ref HEAD") is not { Code: 0 } res)
        {
            return (true, 0);
        }
        var branch = res.Out.Trim();
        if (branch is "master" or "main")
        {
            Logger.Fail($"Ветка '{branch}' запрещена! Используй 'develop'.");
            return (false, 0);
        }
        Logger.Success($"Активная ветка: {branch}.");
        return (true, 0);
    }
    public override async Task<(bool Ok, int Points)> CheckStyleAsync() =>
        await CStyleManager.CheckAndApplyStyleAsync(ProjectPath, Files, RunAsync);
    public override async Task<(bool Ok, int Points)> BuildAsync()
    {
        var (ok, dirs) = await MakefileRunner.RunSequenceAsync(ProjectPath);
        _makeDirs = dirs;
        return (ok, 0);
    }
    public override async Task<(bool Ok, int Points)> CheckMemoryAsync()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Logger.Warning("Valgrind не поддерживается в Windows. Проверка пропущена.");
            return (true, 0);
        }
        return (await ValgrindAnalyzer.CheckAsync(ProjectPath, RunAsync), 0);
    }
    public override async Task<(bool Ok, int Points)> CheckCpuAsync()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Logger.Warning("GCOV не настроен для Windows. Анализ пропущен.");
            return (true, 0);
        }
        return (await CoverageAnalyzer.AnalyzeAsync(ProjectPath, RunAsync), 0);
    }
    public override async Task<(bool Ok, int Points)> CheckAntiCheatAsync()
    {
        Logger.Header("ЭТАП 0.5: АНТИ-ЧИТ");
        var cFiles = Files.Where(f => f.EndsWith(".c", StringComparison.OrdinalIgnoreCase) && !f.Contains("test")).ToList();
        var allOk = true;
        foreach (var f in cFiles)
        {
            var lines = await File.ReadAllLinesAsync(f);
            foreach (var (line, index) in lines.Select((l, i) => (l.TrimStart(), i + 1)))
            {
                if (line.StartsWith("//") || line.StartsWith("/*") || line.StartsWith('*'))
                {
                    continue;
                }
                if (BannedFunctionsRegex().IsMatch(line))
                {
                    Logger.Fail($"[{Path.GetFileName(f)}:{index}] Запрещенная стандартная функция!");
                    allOk = false;
                }
            }
        }
        if (allOk)
        {
            Logger.Success("Код чист от стандартных функций.");
        }
        return (allOk, 0);
    }
    public override async Task<(bool Ok, int Points)> CheckStructureAsync()
    {
        Logger.Header("ЭТАП 5: СТРУКТУРНЫЙ АНАЛИЗ");
        var cFiles = Files.Where(f => f.EndsWith(".c", StringComparison.OrdinalIgnoreCase)).ToList();
        var allOk = true;
        foreach (var f in cFiles)
        {
            var (depth, linesCount, inFunc) = (0, 0, false);
            var content = await File.ReadAllLinesAsync(f);
            foreach (var (line, index) in content.Select((l, i) => (l, i + 1)))
            {
                if (GotoRegex().IsMatch(line))
                {
                    Logger.Fail($"[{Path.GetFileName(f)}:{index}] Нашелся 'goto'!");
                    allOk = false;
                }
                if (line.Contains('{'))
                { if (depth++ == 0) { inFunc = true; linesCount = 0; } }
                if (inFunc)
                {
                    linesCount++;
                }
                if (line.Contains('}'))
                {
                    if (--depth == 0 && inFunc)
                    {
                        if (linesCount > 50)
                        { Logger.Fail($"[{Path.GetFileName(f)}] Функция > 50 строк!"); allOk = false; }
                        inFunc = false;
                    }
                }
                if (depth > 4)
                { Logger.Fail($"[{Path.GetFileName(f)}:{index}] Глубина вложенности > 4!"); allOk = false; }
            }
        }
        return (allOk, 0);
    }
    public override async Task<bool> StripCommentsAsync()
    {
        Logger.Header("ЭТАП 6: ОЧИСТКА");
        var cFiles = Files.Where(f => f.EndsWith(".c") || f.EndsWith(".h")).ToList();
        var toUpdate = new Dictionary<string, string>();
        foreach (var f in cFiles)
        {
            var original = await File.ReadAllTextAsync(f);
            var cleaned = EmptyLinesRegex().Replace(
                CommentsRegex().Replace(original, m => m.Groups[1].Success ? m.Value : ""),
                string.Empty);
            if (original != cleaned)
            {
                toUpdate[f] = cleaned;
            }
        }
        if (toUpdate.Count > 0)
        {
            Console.Write($" {Settings.Colors.LycorisAccent}Вырезать комментарии из {toUpdate.Count} файлов? [y/N]: {Settings.Colors.Reset}");
            if (Console.ReadLine()?.Trim().ToLowerInvariant() == "y")
            {
                foreach (var (path, text) in toUpdate)
                {
                    await File.WriteAllTextAsync(path, text);
                }
                Logger.Success("Очистка завершена.");
            }
        }
        else
        {
            Logger.Info("Комментарии отсутствуют.");
        }
        return true;
    }
    public override async Task<bool> CleanupAsync()
    {
        Logger.Header("ФИНАЛ: ОЧИСТКА");
        foreach (var dir in _makeDirs)
        {
            _ = await RunAsync("make", "clean", dir);
        }
        var cache = Path.Combine(ProjectPath, ".uni-cache");
        if (Directory.Exists(cache))
        {
            Directory.Delete(cache, true);
        }
        Logger.Success("Система очищена.");
        return true;
    }
}
