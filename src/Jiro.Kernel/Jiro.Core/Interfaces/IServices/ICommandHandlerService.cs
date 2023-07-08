namespace Jiro.Core.Interfaces.IServices;

public interface ICommandHandlerService
{
    public event Action<string, object[]> OnLog;
    Task<CommandResponse> ExecuteCommandAsync(IServiceProvider scopedProvider, string prompt);
}