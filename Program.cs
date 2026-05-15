using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
[assembly: SupportedOSPlatform("windows")]
[assembly: SupportedOSPlatform("linux")]
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    var handle = NativeMethods.GetStdHandle(NativeMethods.StdOutputHandle);
    if (NativeMethods.GetConsoleMode(handle, out var mode))
    {
        _ = NativeMethods.SetConsoleMode(handle, mode | NativeMethods.VirtualTerminalProcessing);
    }
}
if (args.Length > 0)
{
    var command = args[0].ToLowerInvariant();
    if (command == "help")
    {
        Console.WriteLine($"\n{Settings.Colors.Bold}{Settings.AppName} v{Settings.Version}{Settings.Colors.Reset}");
        Console.WriteLine("Использование:");
        Console.WriteLine("  uni-sentinel             -> Запустить проверку проекта");
        Console.WriteLine("  uni-sentinel install     -> Установить Сентинель и все C/C++ компиляторы");
        Console.WriteLine("  uni-sentinel update      -> Обновить утилиту из GitHub");
        Console.WriteLine("  uni-sentinel uninstall   -> Полностью удалить Uni-Sentinel");
        Console.WriteLine("  uni-sentinel dump        -> Умный дамп кода (C/C++ или C#)");
        Console.WriteLine("  uni-sentinel install-hook -> Защитить репозиторий (Git Pre-commit)");
        Console.WriteLine("  uni-sentinel ac on       -> Включить режим Анти-Чит");
        Console.WriteLine("  uni-sentinel ac off      -> Выключить режим Анти-Чит");
        return;
    }
    if (command == "install")
    {
        await InstallToSystemAsync();
        return;
    }
    if (command == "uninstall")
    {
        Logger.Header("УДАЛЕНИЕ UNI-SENTINEL");
        Console.Write($"{Settings.Colors.Warning}Вы уверены, что хотите полностью удалить программу и ваш прогресс (XP)? [y/N]: {Settings.Colors.Reset}");
        if (Console.ReadLine()?.Trim().ToLowerInvariant() == "y")
        {
            try
            {
                var isWin = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (isWin)
                {
                    var binPath = Path.Combine(home, ".uni-sentinel", "bin", "uni-sentinel.exe");
                    if (File.Exists(binPath))
                    {
                        await Task.Run(() => File.Delete(binPath));
                    }
                }
                else
                {
                    var info = new ProcessStartInfo("sudo", "rm -f /usr/local/bin/uni-sentinel") { UseShellExecute = false };
                    using var p = Process.Start(info);
                    if (p != null)
                    {
                        await p.WaitForExitAsync();
                    }
                }
                var oldScoreFile = Path.Combine(home, ".uni-sentinel-score");
                var oldConfigFile = Path.Combine(home, ".uni-sentinel-config");
                var moduleDir = Path.Combine(home, ".uni-sentinel");
                var newConfigDir = Path.Combine(home, ".uni_config");
                if (File.Exists(oldScoreFile))
                {
                    await Task.Run(() => File.Delete(oldScoreFile));
                }
                if (File.Exists(oldConfigFile))
                {
                    await Task.Run(() => File.Delete(oldConfigFile));
                }
                if (Directory.Exists(moduleDir))
                {
                    await Task.Run(() => Directory.Delete(moduleDir, true));
                }
                if (Directory.Exists(newConfigDir))
                {
                    await Task.Run(() => Directory.Delete(newConfigDir, true));
                }
                Logger.Success("Uni-Sentinel успешно удален. Прощай, Shadow Monarch...");
            }
            catch (Exception ex) { Logger.Fail($"Ошибка при удалении: {ex.Message}"); }
        }
        else
        { Logger.Info("Удаление отменено."); }
        return;
    }
    if (command == "install-hook")
    {
        Logger.Header("ИНТЕГРАЦИЯ В GIT");
        await GitHookManager.InstallAsync(Directory.GetCurrentDirectory());
        return;
    }
    if (command == "dump")
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
        var solutionFile = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.slnx").FirstOrDefault()
                           ?? Directory.GetFiles(Directory.GetCurrentDirectory(), "*.sln").FirstOrDefault();
        var projectName = solutionFile != null ? Path.GetFileNameWithoutExtension(solutionFile) : new DirectoryInfo(Directory.GetCurrentDirectory()).Name;
        var outputFile = $"{projectName}_total_dump.txt";
        if (File.Exists(outputFile))
        { File.Delete(outputFile); Logger.Warning("Предыдущий дамп стерт."); }
        var excludeDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "bin", "obj", ".git", ".vs", ".vscode", "node_modules", "BuildCache", ".uni-cache", ".uni-sentinel"
        };
        var excludeExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            ".exe", ".dll", ".pdb", ".so", ".dbg", ".app", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".pdf", ".zip", ".7z", ".svg", ".rar"
        };
        Logger.Info("Сканирование всех файлов проекта...");
        var allFiles = Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.*", SearchOption.AllDirectories)
        .Where(file =>
        {
            var relPath = Path.GetRelativePath(Directory.GetCurrentDirectory(), file);
            var fileName = Path.GetFileName(file);
            var parts = relPath.Split(Path.DirectorySeparatorChar);
            if (parts.Any(p => excludeDirs.Contains(p)))
                return false;
            if (excludeExtensions.Contains(Path.GetExtension(file)))
                return false;
            return fileName != outputFile && fileName != ".gitignore";
        }).ToList();
        await using var writer = new StreamWriter(outputFile, append: false, System.Text.Encoding.UTF8);
        await writer.WriteLineAsync($"=== UNI-SENTINEL TOTAL DUMP: {projectName} ===");
        await writer.WriteLineAsync($"Date: {DateTime.Now}");
        await writer.WriteLineAsync($"Files Count: {allFiles.Count}");
        await writer.WriteLineAsync($"Root: {Directory.GetCurrentDirectory()}\n");
        foreach (var file in allFiles)
        {
            var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), file);
            await writer.WriteLineAsync($"\n========================================");
            await writer.WriteLineAsync($"FILE: {relativePath}");
            await writer.WriteLineAsync($"========================================");
            try
            {
                var content = await File.ReadAllTextAsync(file);
                await writer.WriteLineAsync(content);
            }
            catch (Exception ex)
            {
                await writer.WriteLineAsync($"[ERR] Не удалось прочитать файл: {ex.Message}");
            }
        }
        Logger.Success($"Total Dump сохранен в '{outputFile}' ({allFiles.Count} файлов)!");
        return;
    }
    if (command == "update")
    {
        Logger.Header("МГНОВЕННОЕ ОБНОВЛЕНИЕ");
        var isWin = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var url = isWin
            ? "https://github.com/irovbyte/Uni-Sentinel/releases/latest/download/uni-sentinel-win.exe"
            : "https://github.com/irovbyte/Uni-Sentinel/releases/latest/download/uni-sentinel-linux";
        var currentExe = Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrEmpty(currentExe))
        {
            return;
        }
        await UIHelper.RunWithLoadingAsync("Загрузка новейшего ядра Sentinel...", async () =>
        {
            try
            {
                using var client = new HttpClient();
                var newData = await client.GetByteArrayAsync(url);
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
            }
        });
        Logger.Success("Ядро системы обновлено мгновенно! Shadow Monarch готов к бою.");
        return;
    }
    if (command == "ac" && args.Length > 1)
    {
        if (args[1] == "on")
        {
            SettingsManager.SetAntiCheat(true);
        }
        else if (args[1] == "off")
        {
            SettingsManager.SetAntiCheat(false);
        }
        else
        {
            Logger.Fail("Неверный аргумент. Используйте 'ac on' или 'ac off'.");
        }
        return;
    }
    Logger.Fail($"Неизвестная команда: {command}. Введите 'uni-sentinel help'.");
    return;
}
ScoreManager.PrintRankBanner();
ScoreManager.UpdateStreak();
var scanner = new Scanner(Directory.GetCurrentDirectory());
var handler = scanner.DetectHandler();
if (handler == null) { return; }
if (!await handler.CheckDependenciesAsync()) { return; }
var allPassed = true;
if (await handler.CheckGitAsync() is { Ok: false }) { allPassed = false; }
if (SettingsManager.IsAntiCheatEnabled() && await handler.CheckAntiCheatAsync() is { Ok: false }) { allPassed = false; }
if (await handler.CheckStyleAsync() is { Ok: false }) { allPassed = false; }
if (await handler.BuildAsync() is { Ok: false }) { allPassed = false; }
if (await handler.CheckMemoryAsync() is { Ok: false }) { allPassed = false; }
if (await handler.CheckCpuAsync() is { Ok: false }) { allPassed = false; }
if (await handler.CheckStructureAsync() is { Ok: false }) { allPassed = false; }
await handler.StripCommentsAsync();
await handler.CleanupAsync();
if (allPassed)
{
    Logger.Success("РЕЙД ЗАВЕРШЕН ИДЕАЛЬНО! Начислено +1 XP.");
    ScoreManager.AddPoints(1);
}
else
{
    Logger.Fail("МИССИЯ ПРОВАЛЕНА. Ошибки не прощаются (0 XP).");
    Environment.Exit(1);
}
static async Task InstallToSystemAsync()
{
    Logger.Header("УСТАНОВКА UNI-SENTINEL И ОКРУЖЕНИЯ");
    var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
    if (string.IsNullOrEmpty(exePath))
    {
        return;
    }
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        string[] packages = ["Git.Git", "LLVM.LLVM", "WinLibs.GCC"];
        foreach (var pkg in packages)
        {
            await UIHelper.RunWithLoadingAsync($"Установка пакета: {pkg}...", async () =>
            {
                var psi = new ProcessStartInfo("winget", $"install --id {pkg} --silent --accept-package-agreements --accept-source-agreements --disable-interactivity")
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                try
                {
                    using var p = Process.Start(psi);
                    await p!.WaitForExitAsync();
                }
                catch { }
            });
            Logger.Success($"{pkg} успешно загружен.");
        }
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
internal static partial class NativeMethods
{
    internal const int StdOutputHandle = -11;
    internal const uint VirtualTerminalProcessing = 0x0004;
    [LibraryImport("kernel32", SetLastError = true)]
    internal static partial nint GetStdHandle(int nStdHandle);
    [LibraryImport("kernel32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetConsoleMode(nint hConsoleHandle, out uint lpMode);
    [LibraryImport("kernel32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetConsoleMode(nint hConsoleHandle, uint dwMode);
}
