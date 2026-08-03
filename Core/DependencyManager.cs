namespace UniSentinel.Core;

public sealed class ToolDef
{
    public string Id { get; init; } = "";
    public string Stack { get; init; } = "";
    public string Apt { get; init; } = "";
    public string Pacman { get; init; } = "";
    public string WinPortableUrl { get; init; } = "";
}

[JsonSerializable(typeof(ToolDef[]))]
internal sealed partial class AppJsonContext : JsonSerializerContext { }

public static class DependencyManager
{
    private const string RegistryUrl = "https://raw.githubusercontent.com/irovbyte/Uni-Sentinel/main/dependencies.json";

    public static string ToolsDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".uni-sentinel", "tools");

    public static void RefreshSystemPath()
    {
    }

    public static async Task<bool> CheckToolAsync(string toolName)
    {
        var checkCmd = "which";
        var pInfo = new ProcessStartInfo(checkCmd, toolName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        try
        {
            using var p = Process.Start(pInfo);
            if (p is null)
            {
                return false;
            }
            await p.WaitForExitAsync();
            return p.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> RequireStackAsync(string stackName)
    {
        RefreshSystemPath();
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

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Logger.Fail("Установка зависимостей поддерживается только на Linux/WSL.");
            return false;
        }

        var stackTools = tools.Where(t => t.Stack.Equals(stackName, StringComparison.OrdinalIgnoreCase)).ToList();
        List<ToolDef> missingTools = [];

        foreach (var tool in stackTools)
        {
            if (!await CheckToolAsync(tool.Id))
            {
                missingTools.Add(tool);
            }
        }

        if (missingTools.Count == 0)
        {
            return true;
        }

        Logger.Header("АВТО-ИНСТАЛЛЯТОР");
        Logger.Warning($"Missing: {Settings.Colors.Fail}{string.Join(", ", missingTools.Select(t => t.Id))}{Settings.Colors.Reset}");

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

        var pkgList = missingTools.Select(t => manager == "pacman" ? t.Pacman : t.Apt).Distinct();
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
            _ => "dnf"
        };
        return ("Linux", manager);
    }
}
