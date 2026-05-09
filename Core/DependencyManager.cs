using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UniSentinel.Core;

public sealed class ToolDef
{
    public string Id { get; init; } = "";
    public string Stack { get; init; } = "";
    public string Apt { get; init; } = "";
    public string Pacman { get; init; } = "";
    public string Winget { get; init; } = "";
}

[JsonSerializable(typeof(ToolDef[]))]
internal sealed partial class AppJsonContext : JsonSerializerContext { }

public static class DependencyManager
{
    private const string RegistryUrl = "https://raw.githubusercontent.com/irovbyte/Uni-Sentinel/main/dependencies.json";

    public static async Task<bool> RequireStackAsync(string stackName)
    {
        Logger.Info($"Синхронизация Cloud Registry [{stackName.ToUpper()}]...");
        ToolDef[]? tools;
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var json = await client.GetStringAsync(RegistryUrl);
            tools = JsonSerializer.Deserialize(json, AppJsonContext.Default.ToolDefArray);
        }
        catch
        {
            Logger.Warning("Cloud Registry недоступен. Работаю в автономном режиме.");
            return true;
        }

        if (tools is null)
        {
            return true;
        }

        var stackTools = tools.Where(t => t.Stack.Equals(stackName, StringComparison.OrdinalIgnoreCase));
        List<ToolDef> missingTools = [];
        var isWin = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        foreach (var tool in stackTools)
        {
            if (isWin && tool.Winget == "NONE")
            {
                continue;
            }

            // ЧИСТЫЙ ФИКС: Для where.exe нужны только ID, никаких -Command
            var checkCmd = isWin ? "where.exe" : "which";
            var args = tool.Id;

            var pInfo = new ProcessStartInfo(checkCmd, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true, // Добавил, чтобы не мусорить в консоль
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using var p = Process.Start(pInfo);
                if (p is not null)
                {
                    await p.WaitForExitAsync();
                    if (p.ExitCode != 0)
                    {
                        missingTools.Add(tool);
                    }
                }
            }
            catch { missingTools.Add(tool); }
        }

        if (missingTools.Count == 0)
        {
            return true;
        }

        Logger.Header("АВТО-ИНСТАЛЛЯТОР");
        Logger.Warning($"Missing: {Settings.Colors.Fail}{string.Join(", ", missingTools.Select(t => t.Id))}{Settings.Colors.Reset}");

        if (isWin)
        {
            var wingetPackages = missingTools.Select(t => t.Winget).Distinct().Where(w => w != "NONE");
            // Упростил команду - winget умеет ставить пачку сразу
            var wingetCmd = $"winget install {string.Join(" ", wingetPackages.Select(p => $"--id {p}"))} --accept-package-agreements --accept-source-agreements";

            Logger.Info("Требуется вмешательство администратора...");
            Console.WriteLine($"\n {Settings.Colors.LycorisAccent}Выполни в PowerShell (Admin):{Settings.Colors.Reset}");
            Console.WriteLine($" {Settings.Colors.Bold}{wingetCmd}{Settings.Colors.Reset}\n");
            return false;
        }

        var (_, manager) = GetDistroInfo();
        if (string.IsNullOrEmpty(manager))
        {
            return false;
        }

        Console.Write($" {Settings.Colors.LycorisAccent}Установить через {manager}? [y/N]: {Settings.Colors.Reset}");
        if (Console.ReadLine()?.Trim().ToLowerInvariant() != "y")
        {
            return false;
        }

        Logger.Info("Запрашиваю sudo...");
        var pkgList = missingTools.Select(t => manager switch
        {
            "pacman" => t.Pacman,
            _ => t.Apt
        }).Distinct();

        var installArgs = manager switch
        {
            "apt-get" => $"install -y {string.Join(" ", pkgList)}",
            "pacman" => $"-S --noconfirm {string.Join(" ", pkgList)}",
            "dnf" => $"install -y {string.Join(" ", pkgList)}",
            _ => ""
        };

        var proc = Process.Start(new ProcessStartInfo("sudo", $"{manager} {installArgs}") { UseShellExecute = false });
        if (proc is not null)
        {
            await proc.WaitForExitAsync();
        }

        return proc?.ExitCode == 0;
    }

    private static (string Name, string Manager) GetDistroInfo()
    {
        if (!File.Exists("/etc/os-release"))
        {
            return ("Unknown", "");
        }
        var lines = File.ReadAllLines("/etc/os-release");
        var idLine = lines.FirstOrDefault(l => l.StartsWith("ID=", StringComparison.OrdinalIgnoreCase)) ?? "";
        var id = idLine.Contains('=') ? idLine.Split('=')[1].Trim('"') : "";

        var manager = id switch
        {
            "ubuntu" or "debian" or "kali" or "mint" => "apt-get",
            "arch" or "manjaro" => "pacman",
            "fedora" or "centos" or "rhel" => "dnf",
            _ => ""
        };
        return ("Linux", manager);
    }
}
