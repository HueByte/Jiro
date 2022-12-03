using Jiro.Core.Commands.Base;

namespace Jiro.Core.Interfaces.IServices
{
    public interface ICommandHandlerService
    {
        Task<CommandResponse> ExecuteCommandAsync(string prompt);
    }
}