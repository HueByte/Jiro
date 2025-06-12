using OpenAI.Chat;

namespace Jiro.Core.Services.MessageCache;

public interface IMessageCacheService
{
	Task AddChatExchangeAsync (ulong instanceId, List<ChatMessage> messages, List<Core.Models.Message> modelMessages);
	void ClearMessageCache ();
	void ClearOldMessages (ulong instanceId, int range);
	int GetChatMessageCount (ulong instanceId);
	Task<List<ChatMessage>?> GetOrCreateChatMessagesAsync (ulong instanceId);
	Task<string?> GetPersonaCoreMessageAsync ();
	void ModifyMessage (string key, string message, int minutes = 30);
}
