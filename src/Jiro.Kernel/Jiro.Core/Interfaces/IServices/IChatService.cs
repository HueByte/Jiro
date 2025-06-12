using Jiro.Core.Models;

namespace Jiro.Core.Interfaces.IServices;

public interface IChatService
{
    Task<OpenAI.Chat.Message> ChatAsync(string prompt, string sessionId);
    Task<string?> CreateChatSessionAsync(string userId);
    Task<ChatSession?> GetSessionAsync(string sessionId);
}