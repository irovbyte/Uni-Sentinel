using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace UniSentinel.Core.Commands;

public sealed class InstallCommand : ICommand
{
    public string Name => "install";
    public string Description => "Установить Сентинель и окружение";

    public async Task ExecuteAsync(string[] args)
    {
        Logger.Header("УСТАНОВКА UNI-SENTINEL И ОКРУЖЕНИЯ");
        var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
        if (string.IsNullOrEmpty(exePath))
        {
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            DependencyManager.RefreshSystemPath();
            await UIHelper.RunWithLoadingAsync("Интеграция ядра в Windows PATH...", async () =>
            {
                var targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".uni-sentinel", "bin");
                _ = Directory.CreateDirectory(targetDir);
                var targetExe = Path.Combine(targetDir, "uni-sentinel.exe");
                var sourceNormalized = Path.GetFullPath(exePath);
                var targetNormalized = Path.GetFullPath(targetExe);

                if (!sourceNormalized.Equals(targetNormalized, StringComparison.OrdinalIgnoreCase))
                {
                    if (File.Exists(targetNormalized))
                    {
                        try
                        {
                            var oldExe = targetNormalized + ".old";
                            if (File.Exists(oldExe))
                            {
                                File.Delete(oldExe);
                            }
                            File.Move(targetNormalized, oldExe);
                        }
                        catch { }
                    }
                    File.Copy(sourceNormalized, targetNormalized, true);
                }

                var scope = EnvironmentVariableTarget.User;
                var oldPath = Environment.GetEnvironmentVariable("Path", scope);
                if (oldPath != null && !oldPath.Contains(targetDir, StringComparison.OrdinalIgnoreCase))
                {
                    Environment.SetEnvironmentVariable("Path", $"{oldPath};{targetDir}", scope);
                }

                await Task.Delay(800);
            });
            Logger.Success("СИСТЕМА ГОТОВА! Перезапустите терминал и используйте команду 'uni-sentinel'.");
            Logger.Info("Зависимости C/C++ скачаются автоматически портативно при первой проверке кода!");
        }
        else
        {
            await UIHelper.RunWithLoadingAsync("Интеграция ядра в Linux (/usr/local/bin)...", async () =>
            {
                try
                {
                    var psi = new ProcessStartInfo("sudo", $"cp \"{exePath}\" /usr/local/bin/uni-sentinel") { UseShellExecute = false };
                    using var p = Process.Start(psi);
                    await p!.WaitForExitAsync();

                    var psiChmod = new ProcessStartInfo("sudo", "chmod +x /usr/local/bin/uni-sentinel") { UseShellExecute = false };
                    using var pChmod = Process.Start(psiChmod);
                    await pChmod!.WaitForExitAsync();
                }
                catch { }
            });
            Logger.Success("Uni-Sentinel успешно установлен на Linux!");
        }
    }
}
