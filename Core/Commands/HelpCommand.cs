using System;
using System.Threading.Tasks;

namespace UniSentinel.Core.Commands;

public sealed class HelpCommand : ICommand
{
    public string Name => "help";
    public string Description => "Показать справку по командам";

    public Task ExecuteAsync(string[] args)
    {
        Console.WriteLine($"\n{Settings.Colors.Bold}{Settings.AppName} v{Settings.Version}{Settings.Colors.Reset}");
        Console.WriteLine("Использование:");
        Console.WriteLine("  uni-sentinel             -> Запустить проверку проекта");
        Console.WriteLine("  uni-sentinel install     -> Установить Сентинель и все C/C++ компиляторы");
        Console.WriteLine("  uni-sentinel update      -> Обновить утилиту из GitHub");
        Console.WriteLine("  uni-sentinel uninstall   -> Полностью удалить Uni-Sentinel");
        Console.WriteLine("  uni-sentinel dump        -> Умный дамп кода с поддержкой исключений");
        Console.WriteLine("  uni-sentinel install-hook -> Защитить репозиторий (Git Pre-commit)");
        Console.WriteLine("  uni-sentinel ac on       -> Включить режим Анти-Чит");
        Console.WriteLine("  uni-sentinel ac off      -> Выключить режим Анти-Чит");
        return Task.CompletedTask;
    }
}
