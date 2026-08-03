namespace UniSentinel.Handlers.Html;

internal sealed class HtmlHandler(string projectDir)
{

    public async Task<bool> CheckUIAsync(int uiFileCount)
    {
        if (uiFileCount == 0)
        {
            return true;
        }

        Console.Write($"\n {Settings.Colors.LycorisAccent}Найдены файлы UI (XAML/HTML/Razor). Использовать чистку кода? [y/N]: {Settings.Colors.Reset}");
        var ans = Console.ReadLine()?.Trim().ToLowerInvariant();
        if (ans != "y")
        {
            Logger.Warning("Чистка UI-файлов пропущена.");
            return true;
        }

        var files = Directory.EnumerateFiles(projectDir, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var hasXaml = files.Any(f => f.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase));
        var hasHtml = files.Any(f => f.EndsWith(".html", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".razor", StringComparison.OrdinalIgnoreCase));

        Logger.Header("ЭТАП: ФОРМАТИРОВАНИЕ UI");

        if (hasXaml)
        {
            Logger.Info("Запуск XamlStyler для .xaml...");
            try
            {
                var psi = new ProcessStartInfo("dotnet", "tool run xstyler -r -d .")
                {
                    WorkingDirectory = projectDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                if (p != null)
                {
                    await p.WaitForExitAsync();
                }
                Logger.Success("XAML отформатирован.");
            }
            catch
            {
                Logger.Fail("Не удалось запустить XamlStyler. Выберите 'Всё и сразу' или 'HTML / Blazor' при установке (uni-sentinel install).");
            }
        }

        if (hasHtml)
        {
            Logger.Info("Запуск Prettier для HTML/Razor...");
            try
            {
                var psi = new ProcessStartInfo("npx", "prettier --write \"**/*.{html,cshtml,razor}\"")
                {
                    WorkingDirectory = projectDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                if (p != null)
                {
                    await p.WaitForExitAsync();
                }
                Logger.Success("HTML/Razor отформатированы.");
            }
            catch
            {
                Logger.Fail("Не удалось запустить Prettier. Убедитесь, что установлен NodeJS и выполнен 'uni-sentinel install'.");
            }
        }

        return true;
    }
}
