using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using UniSentinel.Config;

namespace UniSentinel.Core.Commands;

public sealed class DumpCommand : ICommand
{
    public string Name => "dump";
    public string Description => "Сгенерировать умный дамп кода (.txt) для LLM с поддержкой списков исключений";

    public async Task ExecuteAsync(string[] args)
    {
        Logger.Header("TOTAL PROJECT DUMP");
        try
        {
            if (Process.GetProcessesByName("Code").Length > 0)
            {
                var pInfo = new ProcessStartInfo("code", "-s") { UseShellExecute = false, CreateNoWindow = true };
                using var p = Process.Start(pInfo);
                _ = p?.WaitForExit(500);
            }
        }
        catch { }

        var currentDir = Directory.GetCurrentDirectory();
        var solutionFile = Directory.GetFiles(currentDir, "*.slnx").FirstOrDefault()
                           ?? Directory.GetFiles(currentDir, "*.sln").FirstOrDefault();
        var projectName = solutionFile != null
            ? Path.GetFileNameWithoutExtension(solutionFile)
            : new DirectoryInfo(currentDir).Name;

        var outputFile = $"{projectName}_total_dump.txt";
        if (File.Exists(outputFile))
        {
            File.Delete(outputFile);
            Logger.Warning("Предыдущий дамп стерт.");
        }

        var excludeDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin", "obj", ".git", ".vs", ".vscode", "node_modules", "BuildCache", ".uni-cache", ".uni-sentinel"
        };
        var excludeExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".dll", ".pdb", ".so", ".dbg", ".app",
            ".png", ".jpg", ".jpeg", ".gif", ".ico", ".pdf",
            ".zip", ".7z", ".rar", ".svg",
            ".ttf", ".otf", ".woff", ".woff2",
            ".keystore", ".jks", ".pfx", ".p12",
            ".user", ".suo", ".sln.dotSettings"
        };

        var globalConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".uni-sentinel", "config", "dump_blacklist.json");
        LoadDumpConfig(globalConfigPath, excludeDirs, excludeExtensions);

        var localConfigPath = Path.Combine(currentDir, ".uni-sentinel_dump.json");
        LoadDumpConfig(localConfigPath, excludeDirs, excludeExtensions);

        Logger.Info("Сканирование файлов с учетом черных списков...");

        var allFiles = Directory.EnumerateFiles(currentDir, "*.*", SearchOption.AllDirectories)
    .Where(file =>
        !file.Split(Path.DirectorySeparatorChar).Any(excludeDirs.Contains) &&
        !excludeExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()) &&
        Path.GetFileName(file) is var name &&
        name != outputFile && name != ".gitignore" && name != ".gitattributes" && name != ".uni-sentinel_dump.json")
    .ToList();

        await using var writer = new StreamWriter(outputFile, append: false, System.Text.Encoding.UTF8);
        await writer.WriteLineAsync($"=== UNI-SENTINEL TOTAL DUMP: {projectName} ===");
        await writer.WriteLineAsync($"Date: {DateTime.Now}");
        await writer.WriteLineAsync($"Files Count: {allFiles.Count}");
        await writer.WriteLineAsync($"Root: {currentDir}\n");

        foreach (var file in allFiles)
        {
            var relativePath = Path.GetRelativePath(currentDir, file);
            await writer.WriteLineAsync($"\n========================================");
            await writer.WriteLineAsync($"FILE: {relativePath}");
            await writer.WriteLineAsync($"========================================");
            try
            {
                using var reader = new StreamReader(file);
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    await writer.WriteLineAsync(line);
                }
            }
            catch (Exception ex)
            {
                await writer.WriteLineAsync($"[ERR] Не удалось прочитать файл: {ex.Message}");
            }
        }

        Logger.Success($"Total Dump сохранен в '{outputFile}' ({allFiles.Count} файлов)!");
    }

    private static void LoadDumpConfig(string path, HashSet<string> excludeDirs, HashSet<string> excludeExtensions)
    {
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize(json, DumpConfigJsonContext.Default.DumpConfig);
            if (config != null)
            {
                if (config.ExcludeDirs != null)
                {
                    foreach (var dir in config.ExcludeDirs)
                    {
                        _ = excludeDirs.Add(dir);
                    }
                }
                if (config.ExcludeExtensions != null)
                {
                    foreach (var ext in config.ExcludeExtensions)
                    {
                        _ = excludeExtensions.Add(ext.StartsWith('.') ? ext : $".{ext}");
                    }
                }
                Logger.Info($"Загружены исключения из {Path.GetFileName(path)}");
            }
        }
        catch (Exception ex)
        {
            Logger.Warning($"Не удалось прочитать {Path.GetFileName(path)}: {ex.Message}");
        }
    }
}
