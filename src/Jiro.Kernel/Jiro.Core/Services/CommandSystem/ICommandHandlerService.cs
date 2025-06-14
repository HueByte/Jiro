namespace Jiro.Core.Services.CommandHandler;

public interface ICommandHandlerService
{
    event Action<string, object[]>? OnLog;

    Task<CommandResponse> ExecuteCommandAsync(IServiceProvider scopedProvider, string prompt);
}
