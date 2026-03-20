namespace UniSentinel.Core;

public class Scanner
{
    private readonly string _rootPath;

    public Scanner(string rootPath) => _rootPath = Path.GetFullPath(rootPath);

    public BaseHandler? DetectHandler()
    {
        var exclude = new HashSet<string> { ".git", ".vscode", "materials", "bin", "obj" };
        var projectFiles = new List<string>();

        int cCount = 0, hCount = 0, csCount = 0;
        bool hasDotnetProject = false;

        foreach (var file in Directory.EnumerateFiles(_rootPath, "*.*", SearchOption.AllDirectories))
        {
            if (exclude.Any(e => file.Contains($"{Path.DirectorySeparatorChar}{e}{Path.DirectorySeparatorChar}"))) continue;

            if (file.EndsWith(".c")) cCount++;
            if (file.EndsWith(".h")) hCount++;
            if (file.EndsWith(".cs")) csCount++;
            if (file.EndsWith(".csproj") || file.EndsWith(".sln")) hasDotnetProject = true;

            projectFiles.Add(file);
        }

        var rank = ScoreManager.GetRankInfo();
        if (hasDotnetProject || csCount > 0)
        {
            Console.WriteLine($"{rank.AccentColor}--- АНАЛИЗ ПРОЕКТА (C# / .NET) ---{Settings.Colors.Reset}");
            Console.WriteLine($" {Settings.Colors.Gray}●{Settings.Colors.Reset} Исходный код C#: {csCount} ф.");
            Console.WriteLine($"{rank.AccentColor}----------------------------------{Settings.Colors.Reset}");

            return new CSharpHandler(_rootPath, projectFiles);
        }
        if (cCount > 0)
        {
            Console.WriteLine($"{rank.AccentColor}--- АНАЛИЗ ПРОЕКТА (C / C++) ---{Settings.Colors.Reset}");
            Console.WriteLine($" {Settings.Colors.Gray}●{Settings.Colors.Reset} Исходный код: {cCount} ф.");
            Console.WriteLine($" {Settings.Colors.Gray}●{Settings.Colors.Reset} Заголовки:    {hCount} ф.");
            Console.WriteLine($"{rank.AccentColor}--------------------------------{Settings.Colors.Reset}");

            return new CHandler(_rootPath, projectFiles);
        }

        Logger.Warning("Подходящие файлы проектов не найдены (.c, .h, .cs, .csproj).");
        return null;
    }
}