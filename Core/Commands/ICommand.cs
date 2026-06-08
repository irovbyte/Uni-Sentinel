using System.Threading.Tasks;

namespace UniSentinel.Core.Commands;

public interface ICommand
{
    public string Name { get; }
    public string Description { get; }
    public Task ExecuteAsync(string[] args);
}
