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
        Console.WriteLine("  uni-sentinel              -> Запустить проверку проекта");
        Console.WriteLine("  uni-sentinel install      -> Установить Сентинель и все C/C++ компиляторы");
        Console.WriteLine("  uni-sentinel update       -> Обновить утилиту из GitHub");
        Console.WriteLine("  uni-sentinel uninstall    -> Полностью удалить Uni-Sentinel");
        Console.WriteLine("  uni-sentinel dump         -> Умный дамп кода (C/C++ или C#)");
        Console.WriteLine("  uni-sentinel install-hook -> Защитить репозиторий (Git Pre-commit)");
        Console.WriteLine("  uni-sentinel ac on        -> Включить режим Анти-Чит");
        Console.WriteLine("  uni-sentinel ac off       -> Выключить режим Анти-Чит");
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
            catch (Exception ex)
            {
                Logger.Fail($"Ошибка при удалении: {ex.Message}");
            }
        }
        else
        {
            Logger.Info("Удаление отменено.");
        }
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
        Logger.Header("УМНАЯ ГЕНЕРАЦИЯ ДАМПА ПРОЕКТА");
        Logger.Info("Синхронизация файловой системы (IDE Ping)...");
        try
        {
            var vsCodeRunning = Process.GetProcessesByName("Code").Length > 0;
            if (vsCodeRunning)
            {
                var pInfo = new ProcessStartInfo("code", "-s")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var p = Process.Start(pInfo);
                _ = p?.WaitForExit(500);
            }
            else
            {
                Thread.Sleep(300);
            }
        }
        catch { }
        var solutionFile = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.slnx").FirstOrDefault()
                           ?? Directory.GetFiles(Directory.GetCurrentDirectory(), "*.sln").FirstOrDefault();
        var projectName = solutionFile != null
            ? Path.GetFileNameWithoutExtension(solutionFile)
            : new DirectoryInfo(Directory.GetCurrentDirectory()).Name;
        var outputFile = $"{projectName}_dump.txt";
        if (File.Exists(outputFile))
        {
            File.Delete(outputFile);
            Logger.Warning($"Старый файл '{outputFile}' стерт.");
        }
        var dumpScanner = new Scanner(Directory.GetCurrentDirectory());
        var files = dumpScanner.GetSmartDumpFiles(null);
        await using var writer = new StreamWriter(outputFile, append: false);
        await writer.WriteLineAsync($"=== UNI-SENTINEL SMART DUMP: {projectName} ===");
        await writer.WriteLineAsync($"Date: {DateTime.Now}");
        await writer.WriteLineAsync($"Root: {Directory.GetCurrentDirectory()}\n");
        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), file);
            if (relativePath.Split(Path.DirectorySeparatorChar).Any(part => part.StartsWith('.')))
            {
                continue;
            }
            await writer.WriteLineAsync($"\n========================================");
            await writer.WriteLineAsync($"FILE: {relativePath}");
            await writer.WriteLineAsync($"========================================");
            await writer.WriteLineAsync(await File.ReadAllTextAsync(file));
        }
        Logger.Success($"Свежий дамп проекта '{projectName}' успешно сохранен!");
        return;
    }
    if (command == "update")
    {
        Logger.Header("ГЛОБАЛЬНОЕ ОБНОВЛЕНИЕ ИЗ GITHUB");
        DependencyManager.RefreshSystemPath();
        if (!await DependencyManager.CheckToolAsync("git"))
        {
            Logger.Fail("Git не найден! Команда update требует установленного Git.");
            Logger.Info("Выполни команду 'uni-sentinel install', чтобы система установила всё необходимое.");
            return;
        }
        var currentExe = Process.GetCurrentProcess().MainModule?.FileName;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && currentExe != null && currentExe.Contains(".uni-sentinel"))
        {
            try
            {
                var oldExe = currentExe + ".old";
                if (File.Exists(oldExe))
                {
                    File.Delete(oldExe);
                }
                File.Move(currentExe, oldExe);
            }
            catch {  }
        }
        var repoUrl = "https://github.com/irovbyte/Uni-Sentinel.git";
        var tmpDir = Path.Combine(Path.GetTempPath(), "uni-sentinel-update");
        Logger.Info("Связь с сервером... Скачиваю свежий исходный код.");
        if (Directory.Exists(tmpDir))
        {
            await Task.Run(() => Directory.Delete(tmpDir, true));
        }
        var cloneInfo = new ProcessStartInfo("git", $"clone {repoUrl} \"{tmpDir}\"") { UseShellExecute = false };
        using var clone = Process.Start(cloneInfo);
        if (clone != null)
        {
            await clone.WaitForExitAsync();
        }
        if (clone?.ExitCode == 0)
        {
            Logger.Info("Исходники получены. Запускаю хардкорную пересборку (Native AOT)...");
            var isWin = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var script = isWin ? "pwsh" : "bash";
            if (isWin && !await DependencyManager.CheckToolAsync("pwsh"))
            {
                script = "powershell";
            }
            var argsStr = isWin ? $"-ExecutionPolicy Bypass -File deploy.ps1" : "deploy.sh";
            var rebuildInfo = new ProcessStartInfo(script, argsStr)
            {
                WorkingDirectory = tmpDir,
                UseShellExecute = false
            };
            using var rebuild = Process.Start(rebuildInfo);
            if (rebuild != null)
            {
                await rebuild.WaitForExitAsync();
            }
            if (rebuild?.ExitCode == 0)
            {
                Logger.Success("Новая версия успешно вшита в ядро системы!");
            }
            else
            {
                Logger.Fail("Сборка упала. Проверь, нет ли ошибок в новом коде на GitHub.");
            }
        }
        else
        {
            Logger.Fail("Не удалось достучаться до GitHub. Проверь интернет.");
        }
        Logger.Info("Удаляю временные файлы загрузки...");
        if (Directory.Exists(tmpDir))
        {
            await Task.Run(() => Directory.Delete(tmpDir, true));
        }
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
if (handler == null)
{
    return;
}
if (!await handler.CheckDependenciesAsync())
{
    return;
}
var allPassed = true;
if (await handler.CheckGitAsync() is { Ok: false })
{
    allPassed = false;
}
if (SettingsManager.IsAntiCheatEnabled())
{
    if (await handler.CheckAntiCheatAsync() is { Ok: false })
    {
        allPassed = false;
    }
}
if (await handler.CheckStyleAsync() is { Ok: false })
{
    allPassed = false;
}
if (await handler.BuildAsync() is { Ok: false })
{
    allPassed = false;
}
if (await handler.CheckMemoryAsync() is { Ok: false })
{
    allPassed = false;
}
if (await handler.CheckCpuAsync() is { Ok: false })
{
    allPassed = false;
}
if (await handler.CheckStructureAsync() is { Ok: false })
{
    allPassed = false;
}
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
        string[] packages = ["Git.Git", "LLVM.LLVM", "GNU.MinGW-w64"];
        foreach (var pkg in packages)
        {
            await RunWithLoading($"Установка пакета: {pkg}...", async () =>
            {
                var psi = new ProcessStartInfo("winget", $"install --id {pkg} --silent --accept-package-agreements --accept-source-agreements")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
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
        await RunWithLoading("Интеграция ядра в Windows PATH...", async () =>
        {
            var targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".uni-sentinel", "bin");
            _ = Directory.CreateDirectory(targetDir);
            var targetExe = Path.Combine(targetDir, "uni-sentinel.exe");
            File.Copy(exePath, targetExe, true);
            var scope = EnvironmentVariableTarget.User;
            var oldPath = Environment.GetEnvironmentVariable("Path", scope);
            if (oldPath != null && !oldPath.Contains(targetDir))
            {
                Environment.SetEnvironmentVariable("Path", $"{oldPath};{targetDir}", scope);
            }
            await Task.Delay(800);
        });
        Logger.Success("СИСТЕМА ГОТОВА! Перезапустите терминал и используйте команду 'uni-sentinel'.");
    }
    else
    {
        await RunWithLoading("Интеграция ядра в Linux (/usr/local/bin)...", async () =>
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
static async Task RunWithLoading(string message, Func<Task> task)
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
