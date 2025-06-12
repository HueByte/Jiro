namespace Jiro.Core.Services.Conversation;

public interface IPersonalizedConversationService
{
	Task<string> ChatAsync (string instanceId, string userId, string message);
	Task<string> ExchangeMessageAsync (string message);
}
