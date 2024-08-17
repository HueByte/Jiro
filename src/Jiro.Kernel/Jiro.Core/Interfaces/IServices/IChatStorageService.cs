
using Jiro.Core.Models;
using Jiro.Core.Services.Chat.Models;

namespace Jiro.Core.Interfaces.IServices;

public interface IChatStorageService
{
    Task<ChatSession> CreateSessionAsync(string ownerId);
    Task<MemorySession?> GetSessionAsync(string sessionId, bool withMessages = false);
    Task<List<string>> GetSessionIdsAsync(string ownerId);
    Task RemoveSessionAsync(string sessionId);
    Task UpdateSessionAsync(ChatSession session);
    Task AppendMessageAsync(string sessionId, string role, string content);
    Task AppendMessagesAsync(string sessionId, IEnumerable<OpenAI.Chat.Message> messages);
}