using System.Collections.Frozen;
using UniSentinel.Handlers.C;
namespace UniSentinel.Core;

internal sealed class Scanner(string rootPath)
{
    private readonly string _root = Path.GetFullPath(rootPath);
    private static readonly FrozenSet<string> t_excludeDirs = new[]
    {
        ".git", ".vscode", "materials", "bin", "obj", ".uni-sentinel", "BuildCache", ".uni-cache"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    private static readonly FrozenSet<string> t_targetExtensions = new[]
    {
        ".cs", ".c", ".h", ".cpp", ".hpp", ".csproj", ".sln", ".slnx", ".json",
    ".sh", ".ps1", ".yml", ".yaml"
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
        .Where(file => t_targetExtensions.Contains(Path.GetExtension(file)) ||
                       Path.GetFileName(file) is "Makefile")];
        return _cachedFiles;
    }
    public BaseHandler? DetectHandler()
    {
        var files = GetProjectFiles();
        var (cs, c, h) = (0, 0, 0);
        var hasDotnet = false;
        foreach (var f in files)
        {
            var ext = Path.GetExtension(f).ToLowerInvariant();
            switch (ext)
            {
                case ".cs":
                    cs++;
                    break;
                case ".c" or ".cpp":
                    c++;
                    break;
                case ".h" or ".hpp":
                    h++;
                    break;
                case ".csproj" or ".slnx" or ".sln":
                    hasDotnet = true;
                    break;
                default:
                    break;
            }
        }
        var (_, _, accent, _, _) = ScoreManager.GetRankInfo();
        if (hasDotnet || cs > 0)
        {
            PrintHeader("C# / .NET 10", accent, [$"Исходный код C#: {cs} ф."]);
            return new CSharpHandler(_root, files);
        }
        if (c > 0)
        {
            PrintHeader("C / C++", accent, [$"Исходный код: {c} ф.", $"Заголовки: {h} ф."]);
            return new CHandler(_root, files);
        }
        Logger.Warning("Подходящие файлы проектов не найдены.");
        return null;
    }
    private static void PrintHeader(string title, string color, string[] stats)
    {
        Console.WriteLine($"{color}--- АНАЛИЗ ПРОЕКТА ({title}) ---{Settings.Colors.Reset}");
        foreach (var stat in stats)
        {
            Console.WriteLine($" {Settings.Colors.Gray}●{Settings.Colors.Reset} {stat}");
        }
        Console.WriteLine($"{color}{new string('-', title.Length + 24)}{Settings.Colors.Reset}");
    }
    public List<string> GetSmartDumpFiles(BaseHandler? handler)
    {
        var all = GetProjectFiles();
        return handler switch
        {
            CSharpHandler => [.. all.Where(f => f.EndsWith(".cs") || f.EndsWith(".csproj") || f.EndsWith(".slnx") || f.EndsWith(".json"))],
            CHandler => [.. all.Where(f => f.EndsWith(".c") || f.EndsWith(".h") || Path.GetFileName(f) == "Makefile")],
            _ => all
        };
    }
}
