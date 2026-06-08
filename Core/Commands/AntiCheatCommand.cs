using System.Threading.Tasks;

namespace UniSentinel.Core.Commands;

public sealed class AntiCheatCommand : ICommand
{
    public string Name => "ac";
    public string Description => "Включить/выключить режим Анти-Чит (блокировка опасных C/C++ функций)";

    public Task ExecuteAsync(string[] args)
    {
        if (args.Length > 1)
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
        }
        else
        {
            Logger.Fail("Недостаточно аргументов. Используйте 'ac on' или 'ac off'.");
        }
        return Task.CompletedTask;
    }
}
