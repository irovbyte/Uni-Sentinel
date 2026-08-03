namespace UniSentinel.Handlers.CSharp;

internal sealed partial class CSharpHandler : BaseHandler
{
    [GeneratedRegex(@"(@(?:""[^""]*"")+|""(?:[^""\n\\]+|\\.)*""|'(?:[^'\n\\]+|\\.)*')|//.*|/\*[\s\S]*?\*/", RegexOptions.Compiled)]
    private static partial Regex CommentsRegex();
    [GeneratedRegex(@"^\s+$[\r\n]*", RegexOptions.Multiline)]
    private static partial Regex EmptyLinesRegex();
    private ProjectManager? _projectManager;
    private StyleManager? _styleManager;
    private bool _isInitialized;
    public override string Name => "C# / .NET";

    public override bool CanHandle(List<string> files)
    {
        return files.Any(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                              f.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) ||
                              f.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase)) ||
               files.Any(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
    }

    public override void Initialize(string projectPath, List<string> files)
    {
        base.Initialize(projectPath, files);
        var projectFile = Files.FirstOrDefault(x => x.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
                           ?? Files.FirstOrDefault(x => x.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));

        _projectManager = new ProjectManager(ProjectPath, projectFile ??
            throw new FileNotFoundException("Файл .csproj, .slnx или .sln не найден."));
        _styleManager = new StyleManager(_projectManager);
    }
    private async Task EnsureInitializedAsync()
    {
        if (!_isInitialized && _projectManager != null)
        {
            await _projectManager.InitializeAsync();
            _isInitialized = true;
        }
    }
    public override async Task<(bool Ok, int Points)> CheckStyleAsync()
    {
        await EnsureInitializedAsync();
        var uiFilesCount = Files.Count(f => f.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) ||
                                            f.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
                                            f.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase) ||
                                            f.EndsWith(".razor", StringComparison.OrdinalIgnoreCase));
        if (uiFilesCount > 0)
        {
            var htmlHandler = new Html.HtmlHandler(ProjectPath);
            _ = await htmlHandler.CheckUIAsync(uiFilesCount);
        }
        return await _styleManager!.CheckStyleAsync();
    }
    public override async Task<(bool Ok, int Points)> BuildAsync()
    {
        await EnsureInitializedAsync();
        return await _projectManager!.BuildAsync();
    }
    public override async Task<(bool Ok, int Points)> CheckMemoryAsync()
    {
        await EnsureInitializedAsync();
        return await _projectManager!.CheckMemoryAsync();
    }
    public override async Task<(bool Ok, int Points)> CheckStructureAsync()
    {
        await EnsureInitializedAsync();
        return await _styleManager!.CheckStructureAsync();
    }
    public override async Task<bool> CheckDependenciesAsync()
    {
        _ = await DependencyManager.RequireStackAsync("csharp");
        return true;
    }
    public override async Task<bool> StripCommentsAsync()
    {
        await EnsureInitializedAsync();
        Logger.Header("ЭТАП 6: ОЧИСТКА КОДА ОТ КОММЕНТАРИЕВ");
        var csFiles = Files.Where(x => x.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && !x.Contains("obj", StringComparison.OrdinalIgnoreCase) && !x.Contains("bin", StringComparison.OrdinalIgnoreCase)).ToList();
        if (csFiles.Count == 0)
        {
            return true;
        }
        var filesToClean = new Dictionary<string, string>();
        foreach (var f in csFiles)
        {
            var text = await File.ReadAllTextAsync(f);
            var cleanText = EmptyLinesRegex().Replace(
            CommentsRegex().Replace(text, m => m.Groups[1].Success ? m.Value : ""),
             string.Empty);
            cleanText = EmptyLinesRegex().Replace(cleanText, string.Empty);
            if (text != cleanText)
            {
                filesToClean[f] = cleanText;
            }
        }
        if (filesToClean.Count == 0)
        {
            Logger.Info("Комментарии отсутствуют.");
            return true;
        }
        Console.Write($" {Settings.Colors.LycorisAccent}Удалить комментарии из {filesToClean.Count} файлов .cs? [y/N]: {Settings.Colors.Reset}");
        if (Console.ReadLine()?.Trim().ToLowerInvariant() != "y")
        {
            return true;
        }
        foreach (var kvp in filesToClean)
        {
            await File.WriteAllTextAsync(kvp.Key, kvp.Value);
        }
        Logger.Success($"Комментарии вырезаны из {filesToClean.Count} файлов.");
        return true;
    }
    public override async Task<bool> CleanupAsync()
    {
        await EnsureInitializedAsync();
        return await _projectManager!.CleanupAsync();
    }
}
