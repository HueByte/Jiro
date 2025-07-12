using Jiro.Core.Models;
using Jiro.Core.Services.Conversation.Models;

namespace Jiro.Core.Services.MessageCache;

/// <summary>
/// Defines the contract for managing chat messages, sessions, and message caching operations.
/// </summary>
public interface IMessageManager
{
	/// <summary>
	/// Retrieves all chat sessions associated with the specified instance.
	/// </summary>
	/// <param name="instanceId">The unique identifier of the instance.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains a list of chat sessions.</returns>
	Task<List<ChatSession>> GetChatSessionsAsync(string instanceId);

	/// <summary>
	/// Adds a new chat exchange containing multiple messages to the specified instance.
	/// </summary>
	/// <param name="instanceId">The unique identifier of the instance.</param>
	/// <param name="messages">The list of chat messages with metadata to add.</param>
	/// <param name="modelMessages">The list of model messages to add.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	Task AddChatExchangeAsync(string instanceId, List<ChatMessageWithMetadata> messages, List<Core.Models.Message> modelMessages);

	/// <summary>
	/// Clears all messages from the message cache.
	/// </summary>
	void ClearMessageCache();

	/// <summary>
	/// Removes old messages from the specified session within the given range.
	/// </summary>
	/// <param name="sessionId">The unique identifier of the session.</param>
	/// <param name="range">The number of messages to remove from the beginning.</param>
	void ClearOldMessages(string sessionId, int range);

	/// <summary>
	/// Gets the total count of chat messages in the specified session.
	/// </summary>
	/// <param name="sessionId">The unique identifier of the session.</param>
	/// <returns>The number of messages in the session.</returns>
	int GetChatMessageCount(string sessionId);

	/// <summary>
	/// Retrieves the session with the specified session ID.
	/// </summary>
	/// <param name="sessionId">The unique identifier of the session.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the session or null if not found.</returns>
	Task<Session?> GetSessionAsync(string sessionId);

	/// <summary>
	/// Retrieves an existing chat session or creates a new one if it doesn't exist.
	/// </summary>
	/// <param name="sessionId">The unique identifier of the session.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the session.</returns>
	Task<Session> GetOrCreateChatSessionAsync(string sessionId);

	/// <summary>
	/// Retrieves the core persona message used for AI interactions.
	/// </summary>
	/// <returns>A task that represents the asynchronous operation. The task result contains the persona message or null if not found.</returns>
	Task<string?> GetPersonaCoreMessageAsync();

	/// <summary>
	/// Modifies an existing message in the cache with an expiration time.
	/// </summary>
	/// <param name="key">The unique key identifying the message.</param>
	/// <param name="message">The new message content.</param>
	/// <param name="minutes">The number of minutes until the message expires. Default is 30.</param>
	void ModifyMessage(string key, string message, int minutes = 30);
}
