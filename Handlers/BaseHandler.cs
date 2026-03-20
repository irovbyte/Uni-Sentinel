namespace UniSentinel.Handlers;

public abstract class BaseHandler
{
    protected readonly string ProjectPath;
    protected readonly List<string> Files;

    protected BaseHandler(string projectPath, List<string> files)
    {
        ProjectPath = projectPath;
        Files = files;
    }

    public virtual Task<(bool Ok, int Points)> CheckGitAsync() => Task.FromResult((true, 0));
    public virtual Task<(bool Ok, int Points)> CheckAntiCheatAsync() => Task.FromResult((true, 0));
    public abstract Task<(bool Ok, int Points)> CheckStyleAsync();
    public virtual Task<(bool Ok, int Points)> BuildAsync() => Task.FromResult((true, 0));
    public virtual Task<(bool Ok, int Points)> RunTestsAsync() => Task.FromResult((true, 0));
    public virtual Task<(bool Ok, int Points)> CheckMemoryAsync() => Task.FromResult((true, 0));
    public virtual Task<(bool Ok, int Points)> CheckCpuAsync() => Task.FromResult((true, 0));
    public virtual Task<(bool Ok, int Points)> CheckStructureAsync() => Task.FromResult((true, 0));
    public virtual Task<bool> StripCommentsAsync() => Task.FromResult(true);
    public virtual Task<bool> CleanupAsync() => Task.FromResult(true);
}