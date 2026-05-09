namespace UniSentinel.Handlers.CSharp;
internal sealed class StyleManager(ProjectManager projectManager)
{
    private static async Task<T> RunWithLoadingAsync<T>(string message, Func<Task<T>> task)
    {
        var spinner = new[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
        var counter = 0;
        using var cts = new CancellationTokenSource();
        var loadingTask = Task.Run(async () =>
        {
            while (!cts.IsCancellationRequested)
            {
                Console.Write($"\r {Settings.Colors.AwakeAccent}{spinner[counter % spinner.Length]}{Settings.Colors.Reset} {message} ");
                counter++;
                await Task.Delay(100);
            }
        });
        try
        { return await task(); }
        finally
        {
            cts.Cancel();
            await loadingTask;
            Console.Write("\r" + new string(' ', 80) + "\r");
        }
    }
    public async Task<(bool Ok, int Points)> CheckStyleAsync()
    {
        Logger.Header("ЭТАП 1: СТИЛЬ (GLOBAL SHADOW MODE)");
        var projectDir = projectManager.GetProjectPath();
        var localConfig = Path.Combine(projectDir, ".editorconfig");
        var globalConfig = Path.Combine(CSharpSettings.GlobalConfigPath, ".editorconfig");
        try
        {
            if (File.Exists(globalConfig))
            {
                File.Copy(globalConfig, localConfig, true);
            }
            var (resStyle, resSpace) = await RunWithLoadingAsync("Анализ кода через Sentinel Engine...", async () =>
            {
                var s = await projectManager.RunDotnetAsync("format style --severity error --verify-no-changes");
                var w = await projectManager.RunDotnetAsync("format whitespace --verify-no-changes");
                return (s, w);
            });
            if (resStyle.Code == 0 && resSpace.Code == 0)
            {
                Logger.Success("Стиль C# идеален!");
                return (true, 0);
            }
            Logger.Fail("Нарушены конвенции (явные типы или скобки).");
            Console.Write($" {Settings.Colors.LycorisAccent}Применить Shadow-Fix? [y/N]: {Settings.Colors.Reset}");
            if (Console.ReadLine()?.Trim().ToLowerInvariant() == "y")
            {
                _ = await RunWithLoadingAsync("Реструктуризация проекта...", async () =>
                {
                    _ = await projectManager.RunDotnetAsync("format whitespace");
                    _ = await projectManager.RunDotnetAsync("format style --severity error");
                    return true;
                });
                Logger.Success("Проект реструктуризирован.");
            }
        }
        finally
        {
            if (File.Exists(localConfig))
            {
                File.Delete(localConfig);
            }
        }
        return (true, 0);
    }
    public async Task<(bool Ok, int Points)> CheckStructureAsync()
    {
        Logger.Header("ЭТАП 5: АНАЛИЗАТОРЫ (ROSLYN)");
        if (await RunWithLoadingAsync("Поиск Code Smells...", () =>
            projectManager.RunDotnetAsync("format analyzers --severity info --verify-no-changes")) is { Code: 0 })
        {
            Logger.Success("Анализаторы довольны.");
            return (true, 0);
        }
        Logger.Fail("Найдены структурные проблемы.");
        Console.Write($" {Settings.Colors.LycorisAccent}Исправить автоматически? [y/N]: {Settings.Colors.Reset}");
        if (Console.ReadLine()?.Trim().ToLowerInvariant() == "y")
        {
            _ = await RunWithLoadingAsync("Чистка анализаторами...", () =>
                projectManager.RunDotnetAsync("format analyzers --severity info"));
            Logger.Success("Чистка анализаторами завершена.");
        }
        return (true, 0);
    }
}
