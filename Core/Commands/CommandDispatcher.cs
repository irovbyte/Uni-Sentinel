using System;
using System.Linq;
using System.Threading.Tasks;

namespace UniSentinel.Core.Commands;

public sealed class CommandDispatcher(ICommand[] commands)
{
    public async Task RunAsync(string[] args)
    {
        if (args.Length == 0)
        {
            var audit = commands.FirstOrDefault(c => c.Name == "audit");
            if (audit != null)
            {
                await audit.ExecuteAsync(args);
            }
            return;
        }

        var commandName = args[0].ToLowerInvariant();
        var command = commands.FirstOrDefault(c => c.Name == commandName);

        if (command != null)
        {
            await command.ExecuteAsync(args);
        }
        else
        {
            Logger.Fail($"Неизвестная команда: {commandName}. Введите 'uni-sentinel help'.");
        }
    }
}
