namespace UniSentinel.Handlers;

public interface IProjectHandler
{
    public string Name { get; }
    public bool CanHandle(List<string> files);
    public void Initialize(string projectPath, List<string> files);
    public Task<bool> CheckDependenciesAsync();
    public Task<(bool Ok, int Points)> CheckGitAsync();
    public Task<(bool Ok, int Points)> CheckAntiCheatAsync();
    public Task<(bool Ok, int Points)> CheckStyleAsync();
    public Task<(bool Ok, int Points)> BuildAsync();
    public Task<(bool Ok, int Points)> RunTestsAsync();
    public Task<(bool Ok, int Points)> CheckMemoryAsync();
    public Task<(bool Ok, int Points)> CheckCpuAsync();
    public Task<(bool Ok, int Points)> CheckStructureAsync();
    public Task<bool> StripCommentsAsync();
    public Task<bool> CleanupAsync();
}

internal abstract class BaseHandler : IProjectHandler
{
    protected string ProjectPath { get; private set; } = string.Empty;
    protected List<string> Files { get; private set; } = [];

    public abstract string Name { get; }
    public abstract bool CanHandle(List<string> files);

    public virtual void Initialize(string projectPath, List<string> files)
    {
        ProjectPath = projectPath;
        Files = files;
    }

    public abstract Task<bool> CheckDependenciesAsync();
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
