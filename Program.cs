namespace UniSentinel;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var services = new ServiceCollection();

        _ = services.AddSingleton<IAppEnvironment, AppEnvironment>();
        _ = services.AddTransient<IProjectHandler, CSharpHandler>();
        _ = services.AddTransient<IScanner, Scanner>();
        _ = services.AddSingleton<CommandDispatcher>();
        _ = services.AddTransient<ICommand, HelpCommand>();
        _ = services.AddTransient<ICommand, InstallCommand>();
        _ = services.AddTransient<ICommand, UpdateCommand>();
        _ = services.AddTransient<ICommand, UninstallCommand>();
        _ = services.AddTransient<ICommand, DumpCommand>();
        _ = services.AddTransient<ICommand, GitHookCommand>();
        _ = services.AddTransient<ICommand, AntiCheatCommand>();
        _ = services.AddTransient<ICommand, RunAuditCommand>();

        var provider = services.BuildServiceProvider();

        var env = provider.GetRequiredService<IAppEnvironment>();
        env.Initialize();

        var dispatcher = provider.GetRequiredService<CommandDispatcher>();
        await dispatcher.RunAsync(args);
    }
}


