namespace UniSentinel.Core.Config;

public interface IAppEnvironment
{
    public string RootDir { get; }
    public string ConfigDir { get; }
    public string ModulesDir { get; }
    public string ToolsDir { get; }
    public string BuildCachePath { get; }

    public void Initialize();
    public Task<string> GetOrAskBuildCachePathAsync();
}

public sealed class AppEnvironment : IAppEnvironment
{
    public string RootDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".uni-sentinel");
    public string ConfigDir => Path.Combine(RootDir, "config");
    public string ModulesDir => Path.Combine(RootDir, "modules");
    public string ToolsDir => Path.Combine(RootDir, "tools");

    private string CacheConfigFile => Path.Combine(ConfigDir, "cache_path.txt");

    public string BuildCachePath
    {
        get
        {
            if (File.Exists(CacheConfigFile))
            {
                var p = File.ReadAllText(CacheConfigFile).Trim();
                if (!string.IsNullOrEmpty(p))
                {
                    return p;
                }
            }
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", "uni-sentinel-build");
        }
    }

    public void Initialize()
    {
        _ = Directory.CreateDirectory(RootDir);
        _ = Directory.CreateDirectory(ConfigDir);
        _ = Directory.CreateDirectory(ModulesDir);
        _ = Directory.CreateDirectory(ToolsDir);
    }

    public async Task<string> GetOrAskBuildCachePathAsync()
    {
        if (File.Exists(CacheConfigFile))
        {
            var savedPath = (await File.ReadAllTextAsync(CacheConfigFile)).Trim();
            if (!string.IsNullOrWhiteSpace(savedPath))
            {
                return savedPath;
            }
        }

        var defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", "uni-sentinel-build");
        Console.WriteLine($"\n{Settings.Colors.LycorisAccent}[INIT]{Settings.Colors.Reset} Настройка окружения...");
        Console.Write($"Путь для кэша сборки (bin/obj) [Default: {defaultPath}]: ");
        var input = Console.ReadLine()?.Trim();
        var finalPath = string.IsNullOrWhiteSpace(input) ? defaultPath : input;

        await File.WriteAllTextAsync(CacheConfigFile, finalPath);
        return finalPath;
    }
}
