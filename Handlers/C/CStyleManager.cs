using UniSentinel.Core;
namespace UniSentinel.Handlers.C;

internal static class CStyleManager
{
    private const string UltraConfig = """
        BasedOnStyle: Google
        Standard: c11
        IndentWidth: 4
        ColumnLimit: 110
        AllowShortFunctionsOnASingleLine: Empty
        BreakBeforeBraces: Attach
        PointerAlignment: Right
        AlignConsecutiveAssignments: true
        AlignConsecutiveDeclarations: true
        """;
    public static async Task<(bool Ok, int Points)> CheckAndApplyStyleAsync(
        string projectPath,
        List<string> files,
        Func<string, string, string?, Task<(int Code, string Out, string Err)>> runAsync)
    {
        Logger.Header("ЭТАП 1: УЛЬТРА СТИЛЬ C11 (CLANG-FORMAT)");
        var localConfig = Path.Combine(projectPath, ".clang-format");
        var existingConfigContent = File.Exists(localConfig)
            ? await File.ReadAllTextAsync(localConfig)
            : null;
        await File.WriteAllTextAsync(localConfig, UltraConfig);
        var cFiles = files.Where(f => f.EndsWith(".c", StringComparison.OrdinalIgnoreCase) ||
                                     f.EndsWith(".h", StringComparison.OrdinalIgnoreCase)).ToList();
        if (cFiles.Count == 0)
        {
            await RestoreConfigAsync(localConfig, existingConfigContent);
            return (true, 0);
        }
        var broken = new List<string>();
        foreach (var f in cFiles)
        {
            var (code, _, _) = await runAsync("clang-format", $"--dry-run -Werror \"{f}\"", projectPath);
            if (code != 0)
            {
                broken.Add(f);
            }
        }
        if (broken.Count == 0)
        {
            Logger.Success("Стиль идеален! Соответствует строгим правилам Школы 21.");
            await RestoreConfigAsync(localConfig, existingConfigContent);
            return (true, 0);
        }
        Logger.Fail($"Найдено отклонений от стандарта в {broken.Count} файлах.");
        Console.Write($" {Settings.Colors.LycorisAccent}Исправить автоматически (y) или пропустить (s)? [y/s]: {Settings.Colors.Reset}");
        if (Console.ReadLine()?.Trim().ToLowerInvariant() == "y")
        {
            var filesArgs = string.Join(" ", cFiles.Select(f => $"\"{f}\""));
            _ = await runAsync("clang-format", $"-i -style=file {filesArgs}", projectPath);
            Logger.Success("Стиль железобетонно выровнен.");
        }
        else
        {
            Logger.Warning("Проверка стиля проигнорирована.");
        }
        await RestoreConfigAsync(localConfig, existingConfigContent);
        return (true, 0);
    }
    private static async Task RestoreConfigAsync(string path, string? originalContent)
    {
        if (originalContent is null)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        else
        {
            await File.WriteAllTextAsync(path, originalContent);
        }
    }
}
