using Jiro.Core.Base;

namespace Jiro.Core.Interfaces.IServices
{
    public interface ICommandHandlerService
    {
        Task<CommandResponse> ExecuteCommandAsync(string prompt);
    }
}