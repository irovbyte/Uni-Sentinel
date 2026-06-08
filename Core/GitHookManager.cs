using System.Diagnostics;
using System.Runtime.InteropServices;
namespace UniSentinel.Core;

internal static class GitHookManager
{
    public static async Task InstallAsync(string projectPath)
    {
        var hooksDir = Path.Combine(projectPath, ".git", "hooks");
        if (!Directory.Exists(hooksDir))
        {
            Logger.Fail("Git-директория не найдена. Сначала выполни 'git init'.");
            return;
        }
        var hookPath = Path.Combine(hooksDir, "pre-commit");
        var exePath = Environment.ProcessPath ?? "uni-sentinel";
        var hookContent = $"#!/bin/sh\n\"{exePath}\" check\n";
        await File.WriteAllBytesAsync(hookPath, System.Text.Encoding.UTF8.GetBytes(hookContent));
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                var startInfo = new ProcessStartInfo("chmod", $"+x \"{hookPath}\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                //ghbdtnb

                using var p = Process.Start(startInfo);
                if (p is not null)
                {


                    await p.WaitForExitAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Не удалось установить права на исполнение: {ex.Message}");
            }
        }
        Logger.Success("Sentinel активирован! Твои коммиты под защитой.");
    }
}
