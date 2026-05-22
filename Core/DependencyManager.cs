using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var userPath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User);
            var machinePath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine);

            var localPath = string.Empty;
            if (Directory.Exists(ToolsDir))
            {
                var toolDirs = Directory.GetDirectories(ToolsDir);
                foreach (var dir in toolDirs)
                {
                    var bin = Path.Combine(dir, "bin");
                    if (Directory.Exists(bin))
                    {
                        localPath += $"{bin};";
                    }
                    else if (dir.Contains("mingw", StringComparison.OrdinalIgnoreCase))
                    {
                        var mingwBin = Path.Combine(dir, "bin");
                        if (Directory.Exists(mingwBin))
                        {
                            localPath += $"{mingwBin};";
                        }
                    }
                }
            }

            Environment.SetEnvironmentVariable("PATH", $"{localPath}{userPath};{machinePath}", EnvironmentVariableTarget.Process);
        }
    }

    public static async Task<bool> CheckToolAsync(string toolName)
    {
        var isWin = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var checkCmd = isWin ? "where.exe" : "which";
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

        var isWin = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
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

        if (isWin)
        {
            Logger.Header("СКАЧИВАНИЕ ПОРТАТИВНЫХ ВЕРСИЙ (WINDOWS)");
            _ = Directory.CreateDirectory(ToolsDir);

            foreach (var tool in missingTools)
            {
                var url = tool.WinPortableUrl;
                if (string.IsNullOrEmpty(url))
                {
                    url = tool.Id switch
                    {
                        "gcc" or "g++" or "make" => "https://github.com/brechtsanders/winlibs_mingw/releases/download/13.2.0-11.0.1-msvcrt-r5/winlibs-x86_64-posix-seh-gcc-13.2.0-mingw-w64msvcrt-11.0.1-r5.zip",
                        "clang-format" or "clang" => "https://github.com/llvm/llvm-project/releases/download/llvmorg-17.0.6/LLVM-17.0.6-win64.exe",
                        _ => null
                    };

                    if (url is null)
                    {
                        continue;
                    }
                }

                await UIHelper.RunWithLoadingAsync($"Скачивание портативного инструмента: {tool.Id}...", async () =>
                {
                    try
                    {
                        var tempZip = Path.Combine(Path.GetTempPath(), $"{tool.Id}_portable.zip");
                        using var client = new HttpClient();
                        var data = await client.GetByteArrayAsync(url);
                        await File.WriteAllBytesAsync(tempZip, data);

                        var extractPath = Path.Combine(ToolsDir, tool.Id);
                        if (Directory.Exists(extractPath))
                        {
                            Directory.Delete(extractPath, true);

                        }
                        if (url.EndsWith(".zip"))
                        {
                            ZipFile.ExtractToDirectory(tempZip, extractPath);
                        }

                        File.Delete(tempZip);
                    }
                    catch (Exception ex)
                    {
                        Logger.Fail($"Не удалось установить {tool.Id}: {ex.Message}");
                    }
                });
            }

            RefreshSystemPath();

            try
            {
                var makeFiles = Directory.GetFiles(ToolsDir, "mingw32-make.exe", SearchOption.AllDirectories);
                foreach (var mf in makeFiles)
                {
                    var makeDest = Path.Combine(Path.GetDirectoryName(mf)!, "make.exe");
                    if (!File.Exists(makeDest))
                    {
                        File.Copy(mf, makeDest);
                    }
                }
            }
            catch { }

            Logger.Success("Окружение обновлено. Проверка арсенала...");


            return (await Task.WhenAll(stackTools.Select(t => CheckToolAsync(t.Id)))).All(result => result);

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
