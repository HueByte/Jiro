using Jiro.Core.Services.Conversation.Models;

using OpenAI.Chat;

namespace Jiro.Core.Services.Conversation;

public interface IHistoryOptimizerService
{
	Task<OptimizerResult> OptimizeMessageHistory (int currentTokenCount, List<ChatMessage> messages, ChatMessage? personaMessage = null);
	bool ShouldOptimizeMessageHistory (ChatTokenUsage tokenUsage);
}
