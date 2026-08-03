namespace UniSentinel.Core.Commands;

public sealed class RunAuditCommand(IScanner scanner) : ICommand
{
    public string Name => "audit";
    public string Description => "Запустить проверку проекта (команда по умолчанию)";

    public async Task ExecuteAsync(string[] args)
    {
        ScoreManager.PrintRankBanner();
        ScoreManager.UpdateStreak();

        scanner.Initialize(Directory.GetCurrentDirectory());
        var handler = scanner.DetectHandler();

        if (handler == null)
        {
            return;
        }

        if (!await handler.CheckDependenciesAsync())
        {
            return;
        }

        var allPassed = true;

        if (await handler.CheckGitAsync() is { Ok: false })
        { allPassed = false; }
        if (SettingsManager.IsAntiCheatEnabled() && await handler.CheckAntiCheatAsync() is { Ok: false })
        { allPassed = false; }
        if (await handler.CheckStyleAsync() is { Ok: false })
        { allPassed = false; }
        if (await handler.BuildAsync() is { Ok: false })
        { allPassed = false; }
        if (await handler.CheckMemoryAsync() is { Ok: false })
        { allPassed = false; }
        if (await handler.CheckCpuAsync() is { Ok: false })
        { allPassed = false; }
        if (await handler.CheckStructureAsync() is { Ok: false })
        { allPassed = false; }

        _ = await handler.StripCommentsAsync();
        _ = await handler.CleanupAsync();

        if (allPassed)
        {
            Logger.Success("РЕЙД ЗАВЕРШЕН ИДЕАЛЬНО! Начислено +1 XP.");
            ScoreManager.AddPoints(1);
        }
        else
        {
            Logger.Fail("МИССИЯ ПРОВАЛЕНА. Ошибки не прощаются (0 XP).");
            Environment.Exit(1);
        }
    }
}
