using Jiro.Core.Base;

namespace Jiro.Core.Interfaces.IServices
{
    public interface ICommandHandlerService
    {
        public event Action<string, object[]> OnLog;
        Task<CommandResponse> ExecuteCommandAsync(string prompt);
    }
}