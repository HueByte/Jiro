using OpenAI.Chat;

namespace Jiro.Core.Services.Conversation;

public interface IConversationCoreService
{
	Task<ChatCompletion> ChatAsync (string instanceId, List<ChatMessage> messageHistory, ChatMessage? personaMessage = null);
	Task<string> ExchangeMessageAsync (string message, ChatMessage? developerPersonaChatMessage = null, int tokenLimit = 1200);
}
