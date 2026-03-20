namespace UniSentinel.Handlers;

public class CSharpHandler : BaseHandler
{
    private string? _projectFile;

    public CSharpHandler(string p, List<string> f) : base(p, f)
    {
        _projectFile = Files.FirstOrDefault(x => x.EndsWith(".sln"))
                    ?? Files.FirstOrDefault(x => x.EndsWith(".csproj"));
    }

    private async Task<(int Code, string Out, string Err)> RunDotnetAsync(string args)
    {
        var info = new ProcessStartInfo("dotnet", args)
        {
            WorkingDirectory = ProjectPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var p = Process.Start(info);
        if (p == null) return (1, "", "Failed to start dotnet");
        string o = await p.StandardOutput.ReadToEndAsync();
        string e = await p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();
        return (p.ExitCode, o, e);
    }

    public override async Task<(bool Ok, int Points)> CheckStyleAsync()
    {
        Logger.Header("ЭТАП 1: СТИЛЬ (DOTNET FORMAT)");
        Logger.Info("Анализ стиля Roslyn...");
        var res = await RunDotnetAsync("format whitespace --verify-no-changes");

        if (res.Code == 0)
        {
            Logger.Success("Стиль C# идеален!");
            return (true, 0);
        }

        Logger.Fail("Нарушены конвенции форматирования C#.");
        Console.Write($" {Settings.Colors.LycorisAccent}Исправить автоматически с помощью 'dotnet format'? [y/N]: {Settings.Colors.Reset}");

        if (Console.ReadLine()?.Trim().ToLower() == "y")
        {
            await RunDotnetAsync("format whitespace");
            Logger.Success("Код успешно отформатирован.");
            return (true, 0);
        }

        Logger.Warning("Форматирование пропущено.");
        return (true, 0);
    }

    public override async Task<(bool Ok, int Points)> BuildAsync()
    {
        Logger.Header("ЭТАП 2: СБОРКА ПРОЕКТА (DOTNET BUILD)");

        if (_projectFile == null)
        {
            Logger.Fail("Файл .csproj или .sln не найден!");
            return (false, 0);
        }

        Logger.Info($"Собираем проект: {Path.GetFileName(_projectFile)}...");
        var res = await RunDotnetAsync("build -c Release");

        if (res.Code != 0)
        {
            Logger.Fail("Ошибка сборки MSBuild!");
            var errLines = res.Out.Split('\n').Where(l => l.Contains("error CS") || l.Contains("MSB"));
            foreach (var el in errLines) Console.WriteLine($"   {Settings.Colors.Gray}{el.Trim()}{Settings.Colors.Reset}");
            return (false, 0);
        }

        Logger.Success("Сборка C# проекта завершена успешно.");
        return (true, 0);
    }

    public override async Task<(bool Ok, int Points)> CheckMemoryAsync()
    {
        Logger.Header("ЭТАП 3: БЕЗОПАСНОСТЬ NUGET (ЗАМЕНА VALGRIND)");
        Logger.Info("Проверка зависимостей на известные уязвимости...");

        var res = await RunDotnetAsync("list package --vulnerable");

        if (res.Out.Contains("has no vulnerable packages") || !res.Out.Contains("Project"))
        {
            Logger.Success("Уязвимых NuGet-пакетов не найдено. Проект безопасен.");
            return (true, 0);
        }

        Logger.Fail("ОБНАРУЖЕНЫ УЯЗВИМОСТИ В ЗАВИСИМОСТЯХ!");
        var warnLines = res.Out.Split('\n').Where(l => l.Contains(">"));
        foreach (var el in warnLines) Console.WriteLine($"   {Settings.Colors.Warning}{el.Trim()}{Settings.Colors.Reset}");

        return (false, 0);
    }

    public override async Task<(bool Ok, int Points)> CheckStructureAsync()
    {
        Logger.Header("ЭТАП 5: АНАЛИЗАТОРЫ КОДА (ROSLYN)");
        Logger.Info("Проверка на code smells, неиспользуемые переменные и плохие практики...");

        var res = await RunDotnetAsync("format analyzers --verify-no-changes");

        if (res.Code == 0)
        {
            Logger.Success("Анализаторы Roslyn довольны. Структура отличная.");
            return (true, 0);
        }

        Logger.Fail("Обнаружены структурные проблемы (Code Smells).");
        Console.Write($" {Settings.Colors.LycorisAccent}Попытаться исправить автоматически? [y/N]: {Settings.Colors.Reset}");

        if (Console.ReadLine()?.Trim().ToLower() == "y")
        {
            await RunDotnetAsync("format analyzers");
            Logger.Success("Автоисправление завершено (некоторые вещи придется править руками).");
            return (true, 0);
        }

        return (true, 0);
    }

    public override async Task<bool> StripCommentsAsync()
    {
        Logger.Header("ЭТАП 6: ОЧИСТКА КОДА ОТ КОММЕНТАРИЕВ");

        var csFiles = Files.Where(x => x.EndsWith(".cs") && !x.Contains("obj") && !x.Contains("bin")).ToList();
        if (!csFiles.Any()) return true;

        string pattern = @"(@(?:""[^""]*"")+|""(?:[^""\n\\]+|\\.)*""|'(?:[^'\n\\]+|\\.)*')|//.*|/\*[\s\S]*?\*/";
        var filesToClean = new Dictionary<string, string>();

        foreach (var f in csFiles)
        {
            string text = await File.ReadAllTextAsync(f);
            string cleanText = Regex.Replace(text, pattern, m => m.Groups[1].Success ? m.Value : "");
            cleanText = Regex.Replace(cleanText, @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);

            if (text != cleanText) filesToClean[f] = cleanText;
        }

        if (!filesToClean.Any())
        {
            Logger.Info("Комментарии отсутствуют.");
            return true;
        }

        Console.Write($" {Settings.Colors.LycorisAccent}Удалить комментарии из {filesToClean.Count} файлов .cs? [y/N]: {Settings.Colors.Reset}");
        if (Console.ReadLine()?.Trim().ToLower() != "y") return true;

        foreach (var kvp in filesToClean) await File.WriteAllTextAsync(kvp.Key, kvp.Value);
        Logger.Success($"Комментарии вырезаны из {filesToClean.Count} файлов.");
        return true;
    }

    public override async Task<bool> CleanupAsync()
    {
        Logger.Header("ФИНАЛ: ОЧИСТКА");
        await RunDotnetAsync("clean");
        var dirsToDelete = Directory.GetDirectories(ProjectPath, "*", SearchOption.AllDirectories)
                                    .Where(d => d.EndsWith("bin") || d.EndsWith("obj"));

        foreach (var dir in dirsToDelete)
        {
            try { Directory.Delete(dir, true); } catch { }
        }

        Logger.Success("Папки bin и obj очищены.");
        return true;
    }
}