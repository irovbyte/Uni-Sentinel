if (args.Length > 0)
{
    string command = args[0].ToLower();

    if (command == "help")
    {
        Console.WriteLine($"\n{Settings.Colors.Bold}{Settings.AppName} v{Settings.Version}{Settings.Colors.Reset}");
        Console.WriteLine("Использование:");
        Console.WriteLine("  uni-sentinel           -> Запустить проверку проекта");
        Console.WriteLine("  uni-sentinel update    -> Скачать обновления через Git");
        Console.WriteLine("  uni-sentinel ac on     -> Включить режим Анти-Чит (Запрет printf и др.)");
        Console.WriteLine("  uni-sentinel ac off    -> Выключить режим Анти-Чит");
        return;
    }

    if (command == "update")
    {
        Logger.Header("ГЛОБАЛЬНОЕ ОБНОВЛЕНИЕ ИЗ GITHUB");
        string repoUrl = "https://github.com/irovbyte/Uni-Sentinel.git";
        string tmpDir = "/tmp/uni-sentinel-update";

        Logger.Info("Связь с сервером... Скачиваю свежий исходный код.");
        Process.Start("rm", $"-rf {tmpDir}")?.WaitForExit();
        var clone = Process.Start(new ProcessStartInfo("git", $"clone {repoUrl} {tmpDir}") { UseShellExecute = false });
        clone?.WaitForExit();

        if (clone?.ExitCode == 0)
        {
            Logger.Info("Исходники получены. Запускаю хардкорную пересборку (Native AOT)...");
            var rebuild = Process.Start(new ProcessStartInfo("bash", "deploy.sh")
            {
                WorkingDirectory = tmpDir,
                UseShellExecute = false
            });
            rebuild?.WaitForExit();

            if (rebuild?.ExitCode == 0)
            {
                Logger.Success("Новая версия успешно вшита в ядро системы (/usr/local/bin)!");
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
        Process.Start("rm", $"-rf {tmpDir}")?.WaitForExit();

        return;
    }

    if (command == "ac" && args.Length > 1)
    {
        if (args[1] == "on") SettingsManager.SetAntiCheat(true);
        else if (args[1] == "off") SettingsManager.SetAntiCheat(false);
        else Logger.Fail("Неверный аргумент. Используйте 'ac on' или 'ac off'.");
        return;
    }

    Logger.Fail($"Неизвестная команда: {command}. Введите 'uni-sentinel help'.");
    return;
}
ScoreManager.PrintRankBanner();

if (!await DependencyManager.CheckAndInstallAsync()) return;

var scanner = new Scanner(Directory.GetCurrentDirectory());
var handler = scanner.DetectHandler();
if (handler == null) return;

bool allPassed = true;
var git = await handler.CheckGitAsync();
if (!git.Ok) allPassed = false;
if (SettingsManager.IsAntiCheatEnabled())
{
    var ac = await handler.CheckAntiCheatAsync();
    if (!ac.Ok) allPassed = false;
}
var style = await handler.CheckStyleAsync();
if (!style.Ok) allPassed = false;
var build = await handler.BuildAsync();
if (!build.Ok) allPassed = false;
var mem = await handler.CheckMemoryAsync();
if (!mem.Ok) allPassed = false;
var cpu = await handler.CheckCpuAsync();
if (!cpu.Ok) allPassed = false;
var structure = await handler.CheckStructureAsync();
if (!structure.Ok) allPassed = false;
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
}