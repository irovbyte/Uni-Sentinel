using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace UniSentinel.Core.Commands;

public sealed class UninstallCommand : ICommand
{
    public string Name => "uninstall";
    public string Description => "Полностью удалить систему и сбросить прогресс";

    public async Task ExecuteAsync(string[] args)
    {
        Logger.Header("УДАЛЕНИЕ UNI-SENTINEL");
        Console.Write($"{Settings.Colors.Warning}Вы уверены, что хотите полностью удалить программу и ваш прогресс (XP)? [y/N]: {Settings.Colors.Reset}");

        if (Console.ReadLine()?.Trim().ToLowerInvariant() == "y")
        {
            try
            {
                var isWin = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                if (isWin)
                {
                    var binPath = Path.Combine(home, ".uni-sentinel", "bin", "uni-sentinel.exe");
                    if (File.Exists(binPath))
                    {
                        await Task.Run(() => File.Delete(binPath));
                    }
                }
                else
                {
                    var info = new ProcessStartInfo("sudo", "rm -f /usr/local/bin/uni-sentinel") { UseShellExecute = false };
                    using var p = Process.Start(info);
                    if (p != null)
                    {
                        await p.WaitForExitAsync();
                    }
                }

                var oldScoreFile = Path.Combine(home, ".uni-sentinel-score");
                var oldConfigFile = Path.Combine(home, ".uni-sentinel-config");
                var moduleDir = Path.Combine(home, ".uni-sentinel");
                var newConfigDir = Path.Combine(home, ".uni_config");

                if (File.Exists(oldScoreFile))
                {
                    await Task.Run(() => File.Delete(oldScoreFile));
                }
                if (File.Exists(oldConfigFile))
                {
                    await Task.Run(() => File.Delete(oldConfigFile));
                }
                if (Directory.Exists(moduleDir))
                {
                    await Task.Run(() => Directory.Delete(moduleDir, true));
                }
                if (Directory.Exists(newConfigDir))
                {
                    await Task.Run(() => Directory.Delete(newConfigDir, true));
                }

                Logger.Success("Uni-Sentinel успешно удален.");
            }
            catch (Exception ex)
            {
                Logger.Fail($"Ошибка при удалении: {ex.Message}");
            }
        }
        else
        {
            Logger.Info("Удаление отменено.");
        }
    }
}
