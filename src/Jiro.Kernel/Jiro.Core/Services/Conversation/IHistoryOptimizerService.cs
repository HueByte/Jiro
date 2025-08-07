using Jiro.Core.Services.Conversation.Models;

using OpenAI.Chat;

namespace Jiro.Core.Services.Conversation;

/// <summary>
/// Defines the contract for message history optimization services that manage conversation context and token usage.
/// </summary>
public interface IHistoryOptimizerService
{
	/// <summary>
	/// Optimizes the message history to reduce token usage while preserving conversation context.
	/// </summary>
	/// <param name="currentTokenCount">The current number of tokens in the conversation.</param>
	/// <param name="messages">The list of chat messages to optimize.</param>
	/// <param name="personaMessage">An optional persona message to consider during optimization.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the optimization result with processed messages.</returns>
	Task<OptimizerResult> OptimizeMessageHistory(int currentTokenCount, List<ChatMessage> messages, ChatMessage? personaMessage = null);

	/// <summary>
	/// Determines whether the message history should be optimized based on token usage.
	/// </summary>
	/// <param name="tokenUsage">The current token usage statistics.</param>
	/// <returns>True if the message history should be optimized; otherwise, false.</returns>
	bool ShouldOptimizeMessageHistory(ChatTokenUsage tokenUsage);
}
