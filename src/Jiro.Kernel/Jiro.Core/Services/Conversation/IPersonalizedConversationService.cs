namespace Jiro.Core.Services.Conversation;

public interface IPersonalizedConversationService
{
    Task<string> ChatAsync(ulong instanceId, ulong userId, ulong botId, string message);
    Task<string> ExchangeMessageAsync(string message);
}
