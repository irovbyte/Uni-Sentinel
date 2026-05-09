namespace UniSentinel.Handlers.CSharp;
internal static class CSharpSettings
{
    internal static string ModulePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".uni-sentinel", "modules", "csharp");
    internal static string GlobalConfigPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".uni_config");
    private static string ConfigFile => Path.Combine(ModulePath, "cache_path.txt");
    public static async Task<string> GetOrAskBuildCachePathAsync()
    {
        _ = Directory.CreateDirectory(ModulePath);
        if (File.Exists(ConfigFile))
        {
            var savedPath = (await File.ReadAllTextAsync(ConfigFile)).Trim();
            if (!string.IsNullOrWhiteSpace(savedPath))
            {
                return savedPath;
            }
        }
        var defaultPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? @"C:\BuildCache"
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", "uni-sentinel-build");
        Console.WriteLine($"\n{Settings.Colors.LycorisAccent}[INIT]{Settings.Colors.Reset} Настройка окружения C#...");
        Console.Write($"Путь для кэша сборки (bin/obj) [Default: {defaultPath}]: ");
        var input = Console.ReadLine()?.Trim();
        var finalPath = string.IsNullOrWhiteSpace(input) ? defaultPath : input;
        await File.WriteAllTextAsync(ConfigFile, finalPath);
        return finalPath;
    }
}
