using System;
using System.Threading;
using System.Threading.Tasks;

namespace UniSentinel.Core;

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
        {
            await task();
        }
        finally
        {
            cts.Cancel();
            await loadingTask;
            Console.Write("\r" + new string(' ', 80) + "\r");
        }
    }
}
