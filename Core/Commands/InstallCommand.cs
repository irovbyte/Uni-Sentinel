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

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Logger.Warning("Установка поддерживается только на Linux/WSL.");
            return;
        }

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
