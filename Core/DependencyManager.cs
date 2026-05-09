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
            var checkCmd = isWin ? "where.exe" : "which";
            var args = tool.Id;
            var pInfo = new ProcessStartInfo(checkCmd, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
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
            var wingetPackages = missingTools.Select(t => t.Winget).Distinct().Where(w => w != "NONE").ToList();

            Logger.Header("АВТО-УСТАНОВКА ЗАВИСИМОСТЕЙ");
            Logger.Info("Запрашиваю права администратора для развертывания...");

            foreach (var packageId in wingetPackages)
            {
                await UIHelper.RunWithLoadingAsync($"Установка {packageId}...", async () =>
                {
                    var psi = new ProcessStartInfo("winget")
                    {
                        Arguments = $"install --id {packageId} --exact --silent --accept-package-agreements --accept-source-agreements",
                        UseShellExecute = true,
                        Verb = "runas",
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    try
                    {
                        using var p = Process.Start(psi);
                        if (p != null)
                        {
                            await p.WaitForExitAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Fail($"Не удалось установить {packageId}: {ex.Message}");
                    }
                });
            }

            Logger.Success("Все зависимости установлены! Перезапусти проверку.");
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
    public static class UIHelper
    {
        public static async Task RunWithLoadingAsync(string message, Func<Task> task)
        {
            var spinner = new[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
            var counter = 0;
            using var cts = new CancellationTokenSource();
            var loadingTask = Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    Console.Write($"\r {Settings.Colors.AwakeAccent}{spinner[counter % spinner.Length]}{Settings.Colors.Reset} {message} ");
                    counter++;
                    await Task.Delay(100);
                }
            });

            try
            { await task(); }
            finally
            {
                cts.Cancel();
                await loadingTask;
                Console.Write("\r" + new string(' ', 80) + "\r");
            }
        }
    }
}
