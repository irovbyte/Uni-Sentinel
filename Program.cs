using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using UniSentinel.Core.Commands;

[assembly: SupportedOSPlatform("windows")]
[assembly: SupportedOSPlatform("linux")]

namespace UniSentinel;

public class Program
{
    public static async Task Main(string[] args)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var handle = NativeMethods.GetStdHandle(NativeMethods.StdOutputHandle);
            if (NativeMethods.GetConsoleMode(handle, out var mode))
            {
                _ = NativeMethods.SetConsoleMode(handle, mode | NativeMethods.VirtualTerminalProcessing);
            }
        }

        var commands = new ICommand[]
        {
            new HelpCommand(),
            new InstallCommand(),
            new UpdateCommand(),
            new UninstallCommand(),
            new DumpCommand(),
            new GitHookCommand(),
            new AntiCheatCommand(),
            new RunAuditCommand()
        };

        var dispatcher = new CommandDispatcher(commands);
        await dispatcher.RunAsync(args);
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
