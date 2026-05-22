using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace UniSentinel.Core.Commands;

public sealed class UpdateCommand : ICommand
{
    public string Name => "update";
    public string Description => "Обновить утилиту из GitHub";

    public async Task ExecuteAsync(string[] args)
    {
        Logger.Header("МГНОВЕННОЕ ОБНОВЛЕНИЕ");

        var isWin = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var url = isWin
            ? "https://github.com/irovbyte/Uni-Sentinel/releases/latest/download/uni-sentinel-win.exe"
            : "https://github.com/irovbyte/Uni-Sentinel/releases/latest/download/uni-sentinel-linux";

        var currentExe = Process.GetCurrentProcess().MainModule?.FileName;

        if (string.IsNullOrEmpty(currentExe))
        {
            Logger.Fail("Не удалось определить путь к исполняемому файлу.");
            return;
        }

        await UIHelper.RunWithLoadingAsync("Загрузка новейшего ядра Sentinel...", async () =>
        {
            try
            {
                using var client = new HttpClient();

                using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                _ = response.EnsureSuccessStatusCode();

                var newData = await response.Content.ReadAsByteArrayAsync();

                if (isWin)
                {
                    var oldExe = currentExe + ".old";
                    if (File.Exists(oldExe))
                    {
                        File.Delete(oldExe);
                    }
                    File.Move(currentExe, oldExe);
                    await File.WriteAllBytesAsync(currentExe, newData);
                }
                else
                {
                    await File.WriteAllBytesAsync(currentExe, newData);
                    var chmodInfo = new ProcessStartInfo("chmod", $"+x \"{currentExe}\"") { UseShellExecute = false };
                    using var p = Process.Start(chmodInfo);
                    if (p != null)
                    {
                        await p.WaitForExitAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Fail($"Сбой при обновлении: {ex.Message}");
                Environment.Exit(1);
            }
        });

        Logger.Success("Ядро системы обновлено мгновенно! Shadow Monarch готов к бою.");
    }
}
