namespace UniSentinel.Core;

public interface IScanner
{
    public void Initialize(string rootPath);
    public List<string> GetProjectFiles();
    public IProjectHandler? DetectHandler();
    public List<string> GetSmartDumpFiles(IProjectHandler? handler);
}

internal sealed class Scanner(IEnumerable<IProjectHandler> handlers) : IScanner
{
    private string _root = string.Empty;

    public void Initialize(string rootPath)
    {
        _root = Path.GetFullPath(rootPath);
        _cachedFiles = null;
    }

    private static readonly FrozenSet<string> t_excludeDirs = new[]
    {
        ".git", ".vscode", "materials", "bin", "obj", ".uni-sentinel", "BuildCache", ".uni-cache"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    private static readonly FrozenSet<string> t_targetExtensions = new[]
    {
        ".cs", ".csproj", ".sln", ".slnx", ".json", ".sh", ".yml", ".yaml",
        ".c", ".cpp", ".h"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    private List<string>? _cachedFiles;
    public List<string> GetProjectFiles()
    {
        if (_cachedFiles is not null)
        {
            return _cachedFiles;
        }
        _cachedFiles = [.. Directory.EnumerateFiles(_root, "*.*", SearchOption.AllDirectories)
        .Where(file =>
        {
            var relPath = Path.GetRelativePath(_root, file);
            var parts = relPath.Split(Path.DirectorySeparatorChar);
            return !parts.Any(p => (p.StartsWith('.') && p.Length > 1) || t_excludeDirs.Contains(p));
        })
        .Where(file =>
        {
            var ext = Path.GetExtension(file);
            if (string.IsNullOrEmpty(ext))
            {
                var name = Path.GetFileName(file);
                return name.Equals("Makefile", StringComparison.OrdinalIgnoreCase);
            }
            return t_targetExtensions.Contains(ext);
        })];
        return _cachedFiles;
    }
    public IProjectHandler? DetectHandler()
    {
        var files = GetProjectFiles();

        foreach (var handler in handlers)
        {
            if (handler.CanHandle(files))
            {
                handler.Initialize(_root, files);
                PrintHeader(handler.Name, [$"Найдено файлов для {handler.Name}: {files.Count} ф."]);
                return handler;
            }
        }

        Logger.Warning("Подходящие файлы проектов не найдены.");
        return null;
    }
    private static void PrintHeader(string title, string[] stats)
    {
        var rule = new Spectre.Console.Rule($"[bold]{title}[/]")
        {
            Justification = Spectre.Console.Justify.Left
        };
        Spectre.Console.AnsiConsole.Write(rule);

        foreach (var stat in stats)
        {
            Spectre.Console.AnsiConsole.MarkupLine($" [grey]●[/] {stat}");
        }
        Spectre.Console.AnsiConsole.WriteLine();
    }
    public List<string> GetSmartDumpFiles(IProjectHandler? handler)
    {
        var all = GetProjectFiles();
        return handler?.Name switch
        {
            "C# / .NET" => [.. all.Where(f => f.EndsWith(".cs") || f.EndsWith(".csproj") || f.EndsWith(".slnx") || f.EndsWith(".json"))],
            _ => all
        };
    }
}
