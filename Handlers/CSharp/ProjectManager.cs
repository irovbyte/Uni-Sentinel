namespace UniSentinel.Handlers.CSharp;
internal sealed class ProjectManager(string projectPath, string projectFile)
{
    private readonly string _modulePath = CSharpSettings.ModulePath;
    private string _buildCacheBase = string.Empty;
    public string GetProjectPath() => projectPath;
    public async Task InitializeAsync()
    {
        _buildCacheBase = await CSharpSettings.GetOrAskBuildCachePathAsync();
        _ = Directory.CreateDirectory(_modulePath);
        _ = Directory.CreateDirectory(CSharpSettings.GlobalConfigPath);
        await GenerateGlobalFilesAsync();
        await GenerateShadowPropsAsync();
    }
    private static async Task GenerateGlobalFilesAsync()
    {
        var globalDir = CSharpSettings.GlobalConfigPath;
        await File.WriteAllTextAsync(Path.Combine(globalDir, ".editorconfig"), EditorConfigTemplate.GetContent());
    }
    private async Task GenerateShadowPropsAsync()
    {
        var pathHash = Math.Abs(projectPath.GetHashCode());
        var propsContent = $@"<Project>
  <PropertyGroup>
    <UseArtifactsOutput>true</UseArtifactsOutput>
    <ArtifactsPath>{_buildCacheBase}/$(MSBuildProjectName)_{pathHash}</ArtifactsPath>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <AnalysisLevel>latest</AnalysisLevel>
    <AnalysisMode>All</AnalysisMode>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>";
        await File.WriteAllTextAsync(Path.Combine(_modulePath, "Directory.Build.props"), propsContent);
    }
    public async Task<(int Code, string Out, string Err)> RunDotnetAsync(string args)
    {
        var shadowPropsPath = Path.Combine(_modulePath, "Directory.Build.props");
        var cpuCount = Environment.ProcessorCount;
        var maxThreads = cpuCount <= 4 ? Math.Max(1, cpuCount - 1) : (int)(cpuCount * 0.8);
        var shadowArgs = $"-p:DirectoryBuildPropsPath=\"{shadowPropsPath}\" -maxcpucount:{maxThreads}";
        var info = new ProcessStartInfo("dotnet", $"{args} {shadowArgs}")
        {
            WorkingDirectory = projectPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true 
        };
        using var p = new Process { StartInfo = info };
        _ = p.Start();
        try
        { p.PriorityClass = ProcessPriorityClass.BelowNormal; }
        catch { }
        var o = await p.StandardOutput.ReadToEndAsync();
        var e = await p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();
        return (p.ExitCode, o, e);
    }
    public async Task<(bool Ok, int Points)> BuildAsync()
    {
        Logger.Header("ЭТАП 2: СБОРКА ПРОЕКТА (SHADOW MODE)");
        var pathHash = Math.Abs(projectPath.GetHashCode());
        Logger.Info($"Собираем: {Path.GetFileName(projectFile)} (ID: {pathHash})");
        var (code, output, _) = await RunDotnetAsync("build -c Release");
        if (code != 0)
        {
            Logger.Fail("Ошибка сборки MSBuild!");
            var errLines = output.Split('\n').Where(l =>
                l.Contains("error CS", StringComparison.OrdinalIgnoreCase) ||
                l.Contains("MSB", StringComparison.OrdinalIgnoreCase));
            foreach (var el in errLines)
            {
                Console.WriteLine($"   {Settings.Colors.Gray}{el.Trim()}{Settings.Colors.Reset}");
            }
            return (false, 0);
        }
        Logger.Success("Сборка C# проекта завершена успешно.");
        return (true, 0);
    }
    public async Task<(bool Ok, int Points)> CheckMemoryAsync()
    {
        Logger.Header("ЭТАП 3: БЕЗОПАСНОСТЬ NUGET");
        if (await RunDotnetAsync("list package --vulnerable") is { Out: var output }
            && (output.Contains("has no vulnerable packages") || !output.Contains("Project")))
        {
            Logger.Success("Уязвимых NuGet-пакетов не найдено.");
            return (true, 0);
        }
        Logger.Fail("ОБНАРУЖЕНЫ УЯЗВИМОСТИ В ЗАВИСИМОСТЯХ!");
        var warnLines = output.Split('\n').Where(l => l.Contains('>'));
        foreach (var el in warnLines)
        {
            Console.WriteLine($"   {Settings.Colors.Warning}{el.Trim()}{Settings.Colors.Reset}");
        }
        return (false, 0);
    }
    public async Task<bool> CleanupAsync()
    {
        Logger.Header("ФИНАЛ: ОЧИСТКА");
        _ = await RunDotnetAsync("clean");
        var localDirs = Directory.GetDirectories(projectPath, "*", SearchOption.AllDirectories)
            .Where(d => d.EndsWith("bin", StringComparison.OrdinalIgnoreCase) ||
                        d.EndsWith("obj", StringComparison.OrdinalIgnoreCase));
        foreach (var dir in localDirs)
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
            }
            catch { }
        }
        var pathHash = Math.Abs(projectPath.GetHashCode());
        var projectCache = Path.Combine(_buildCacheBase, $"{Path.GetFileNameWithoutExtension(projectPath)}_{pathHash}");
        try
        {
            if (Directory.Exists(projectCache))
            {
                Directory.Delete(projectCache, true);
            }
        }
        catch { }
        Logger.Success("Изолированный кэш уничтожен. Корень чист.");
        return true;
    }
}
