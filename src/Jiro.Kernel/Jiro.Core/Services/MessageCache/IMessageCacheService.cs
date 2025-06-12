using OpenAI.Chat;

namespace Jiro.Core.Services.MessageCache;

public interface IMessageCacheService
{
	Task AddChatExchangeAsync (string instanceId, List<ChatMessage> messages, List<Core.Models.Message> modelMessages);
	void ClearMessageCache ();
	void ClearOldMessages (string instanceId, int range);
	int GetChatMessageCount (string instanceId);
	Task<List<ChatMessage>?> GetOrCreateChatMessagesAsync (string instanceId);
	Task<string?> GetPersonaCoreMessageAsync ();
	void ModifyMessage (string key, string message, int minutes = 30);
}
