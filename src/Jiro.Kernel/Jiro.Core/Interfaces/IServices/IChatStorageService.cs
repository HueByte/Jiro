
using Jiro.Core.Models;

namespace Jiro.Core.Interfaces.IServices;

public interface IChatStorageService
{
	Task AppendMessagesToSessionAsync (string sessionId, List<Message> messages);
	Task AppendMessageToSessionAsync (string sessionId, string role, string content);
	Task<ChatSession> CreateSessionAsync (string ownerId);
	Task DeleteSessionAsync (string sessionId);
	Task<ChatSession?> GetSessionAsync (string sessionId);
	Task<List<ChatSession>> GetSessionsAsync (string ownerId);
}
