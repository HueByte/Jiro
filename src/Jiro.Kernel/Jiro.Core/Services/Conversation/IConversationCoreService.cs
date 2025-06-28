using OpenAI.Chat;

namespace Jiro.Core.Services.Conversation;

/// <summary>
/// Defines the contract for core conversation services that handle chat interactions and message processing.
/// </summary>
public interface IConversationCoreService
{
	/// <summary>
	/// Conducts a chat conversation using the specified message history and optional persona message.
	/// </summary>
	/// <param name="instanceId">The unique identifier for the conversation instance.</param>
	/// <param name="messageHistory">The list of previous chat messages that form the conversation context.</param>
	/// <param name="personaMessage">An optional persona message to influence the AI's behavior and responses.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the chat completion response.</returns>
	Task<ChatCompletion> ChatAsync(string instanceId, List<ChatMessage> messageHistory, ChatMessage? personaMessage = null);

	/// <summary>
	/// Exchanges a single message with the AI system and receives a response.
	/// </summary>
	/// <param name="message">The message to send to the AI system.</param>
	/// <param name="developerPersonaChatMessage">An optional developer persona message to guide the AI's responses.</param>
	/// <param name="tokenLimit">The maximum number of tokens to use in the response. Default is 1200.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the AI's response as a string.</returns>
	Task<string> ExchangeMessageAsync(string message, ChatMessage? developerPersonaChatMessage = null, int tokenLimit = 1200);
}
