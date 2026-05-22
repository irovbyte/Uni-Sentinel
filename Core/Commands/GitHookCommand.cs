using System;
using System.IO;
using System.Threading.Tasks;

namespace UniSentinel.Core.Commands;

public sealed class GitHookCommand : ICommand
{
    public string Name => "install-hook";
    public string Description => "Защитить репозиторий (Git Pre-commit интеграция)";

    public async Task ExecuteAsync(string[] args)
    {
        Logger.Header("ИНТЕГРАЦИЯ В GIT");
        await GitHookManager.InstallAsync(Directory.GetCurrentDirectory());
    }
}
