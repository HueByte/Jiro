namespace Jiro.Core.Services.Conversation;

/// <summary>
/// Defines the contract for personalized conversation services that handle user-specific chat interactions.
/// </summary>
public interface IPersonalizedConversationService
{
	/// <summary>
	/// Conducts a personalized chat conversation for a specific user and instance.
	/// </summary>
	/// <param name="instanceId">The unique identifier of the conversation instance.</param>
	/// <param name="userId">The unique identifier of the user participating in the conversation.</param>
	/// <param name="message">The user's message to process.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the AI's response as a string.</returns>
	Task<string> ChatAsync(string instanceId, string userId, string message);

	/// <summary>
	/// Exchanges a single message with the AI system in a personalized context.
	/// </summary>
	/// <param name="message">The message to send to the AI system.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the AI's response as a string.</returns>
	Task<string> ExchangeMessageAsync(string message);
}
