using Jiro.Core.Services.GPTService.Models.ChatGPT;

namespace Jiro.Core.Interfaces.IServices
{
    public interface ITokenizerService
    {
        Task<int> GetTokenCountAsync(string text);
        Task<List<ChatMessage>> ReduceTokenCountAsync(List<ChatMessage> messages);
    }
}