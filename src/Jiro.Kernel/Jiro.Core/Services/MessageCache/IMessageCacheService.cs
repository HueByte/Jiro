using Jiro.Core.Models;
using Jiro.Core.Services.Conversation.Models;

using OpenAI.Chat;

namespace Jiro.Core.Services.MessageCache;

public interface IMessageCacheService
{
	Task<List<ChatSession>> GetChatSessionsAsync (string instanceId);
	Task AddChatExchangeAsync (string instanceId, List<ChatMessage> messages, List<Core.Models.Message> modelMessages);
	void ClearMessageCache ();
	void ClearOldMessages (string sessionId, int range);
	int GetChatMessageCount (string sessionId);
	Task<Session> GetOrCreateChatSessionAsync (string sessionId);
	Task<string?> GetPersonaCoreMessageAsync ();
	void ModifyMessage (string key, string message, int minutes = 30);
}
