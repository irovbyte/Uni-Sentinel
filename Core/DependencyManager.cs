namespace UniSentinel.Core;

public static class DependencyManager
{
    public static async Task<bool> CheckAndInstallAsync()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return true;

        string[] requiredTools = { "make", "clang-format", "valgrind", "gcc", "lcov" };
        var missingTools = new List<string>();

        foreach (var tool in requiredTools)
        {
            var info = new ProcessStartInfo("which", tool) { RedirectStandardOutput = true, UseShellExecute = false };
            using var p = Process.Start(info);
            await p!.WaitForExitAsync();
            if (p.ExitCode != 0) missingTools.Add(tool);
        }

        if (!missingTools.Any()) return true;
        var (distroName, packageManager) = GetDistroInfo();

        Logger.Header("СИСТЕМНАЯ ПРОВЕРКА");
        Logger.Warning($"Обнаружен дистрибутив: {Config.Settings.Colors.LycorisAccent}{distroName}{Config.Settings.Colors.Reset}");
        Logger.Warning($"Отсутствуют инструменты: {Config.Settings.Colors.Fail}{string.Join(", ", missingTools)}{Config.Settings.Colors.Reset}");

        if (string.IsNullOrEmpty(packageManager))
        {
            Logger.Fail("Не удалось определить менеджер пакетов для вашего дистрибутива. Установите инструменты вручную.");
            return false;
        }

        Console.Write($" Желаете установить их через {packageManager}? [y/N]: ");
        if (Console.ReadLine()?.Trim().ToLower() == "y")
        {
            Logger.Info($"Запускаю установку через {packageManager}... (введите пароль sudo)");
            string args = packageManager switch
            {
                "apt-get" => $"install -y {string.Join(" ", missingTools)}",
                "pacman" => $"-S --noconfirm {string.Join(" ", missingTools)}",
                "dnf" => $"install -y {string.Join(" ", missingTools)}",
                _ => ""
            };

            var proc = Process.Start(new ProcessStartInfo("sudo", $"{packageManager} {args}") { UseShellExecute = false });
            await proc!.WaitForExitAsync();

            if (proc.ExitCode == 0)
            {
                Logger.Success("Система подготовлена к рейду!");
                return true;
            }
        }

        Logger.Fail("Отказ от установки. Проверка невозможна.");
        return false;
    }

    private static (string Name, string Manager) GetDistroInfo()
    {
        if (File.Exists("/etc/os-release"))
        {
            var lines = File.ReadAllLines("/etc/os-release");
            string id = lines.FirstOrDefault(l => l.StartsWith("ID="))?.Split('=')[1].Replace("\"", "") ?? "";
            string prettyName = lines.FirstOrDefault(l => l.StartsWith("PRETTY_NAME="))?.Split('=')[1].Replace("\"", "") ?? "Unknown Linux";

            string manager = id switch
            {
                "ubuntu" or "debian" or "kali" or "mint" => "apt-get",
                "arch" or "manjaro" => "pacman",
                "fedora" or "centos" or "rhel" => "dnf",
                _ => ""
            };

            return (prettyName, manager);
        }
        return ("Unknown", "");
    }
}