using Jiro.Core.Models;
using Jiro.Core.Services.Conversation.Models;

namespace Jiro.Core.Services.MessageCache;

/// <summary>
/// Composite interface that combines session management and message caching operations.
/// This interface bridges the separated services for backward compatibility while consumers migrate.
/// </summary>
public interface IMessageManager
{
	/// <summary>
	/// Retrieves the session with the specified session ID.
	/// </summary>
	/// <param name="sessionId">The unique identifier of the session.</param>
	/// <param name="includeMessages">Whether to include messages in the result.</param>
	/// <param name="instanceId">The unique identifier of the instance. If null, will be fetched from database.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the session or null if not found.</returns>
	Task<Session?> GetSessionAsync(string sessionId, bool includeMessages = false, string? instanceId = null);

	/// <summary>
	/// Retrieves an existing chat session or creates a new one if it doesn't exist.
	/// </summary>
	/// <param name="sessionId">The unique identifier of the session.</param>
	/// <param name="includeMessages">Whether to include messages in the result.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the session.</returns>
	Task<Session> GetOrCreateChatSessionAsync(string sessionId, bool includeMessages = false);

	/// <summary>
	/// Adds a new chat exchange containing multiple messages to the specified session.
	/// </summary>
	/// <param name="sessionId">The unique identifier of the session.</param>
	/// <param name="messages">The list of chat messages with metadata to add.</param>
	/// <param name="modelMessages">The list of model messages to add.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	Task AddChatExchangeAsync(string sessionId, List<ChatMessageWithMetadata> messages, List<Core.Models.Message> modelMessages);

	/// <summary>
	/// Removes old messages from the specified session within the given range.
	/// </summary>
	/// <param name="sessionId">The unique identifier of the session.</param>
	/// <param name="range">The number of messages to keep (removes messages beyond this count).</param>
	void ClearOldMessages(string sessionId, int range);

	/// <summary>
	/// Clears all cached data for a specific session.
	/// </summary>
	/// <param name="sessionId">The unique identifier of the session.</param>
	void InvalidateSessionCache(string sessionId);

	/// <summary>
	/// Clears all cached sessions for a specific instance.
	/// </summary>
	/// <param name="instanceId">The unique identifier of the instance.</param>
	void InvalidateInstanceSessionsCache(string instanceId);

	/// <summary>
	/// Retrieves the core persona message used for AI interactions.
	/// </summary>
	/// <returns>A task that represents the asynchronous operation. The task result contains the persona message or null if not found.</returns>
	Task<string?> GetPersonaCoreMessageAsync();

	/// <summary>
	/// Retrieves all chat sessions associated with the specified instance.
	/// </summary>
	/// <param name="instanceId">The unique identifier of the instance.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains a list of chat sessions.</returns>
	Task<List<ChatSession>> GetChatSessionsAsync(string instanceId);

	/// <summary>
	/// Clears all message cache entries. Legacy method.
	/// </summary>
	void ClearMessageCache();

	/// <summary>
	/// Modifies a message in the cache with the specified key and expiration. Legacy method.
	/// </summary>
	/// <param name="key">The cache key.</param>
	/// <param name="message">The message content.</param>
	/// <param name="minutes">The expiration time in minutes.</param>
	void ModifyMessage(string key, string message, int minutes);

	/// <summary>
	/// Gets the count of messages in a specific session from cache.
	/// </summary>
	/// <param name="sessionId">The session identifier.</param>
	/// <returns>The number of messages in the session.</returns>
	int GetChatMessageCount(string sessionId);

	/// <summary>
	/// Removes a chat session and all its messages from both the database and cache.
	/// </summary>
	/// <param name="sessionId">The unique identifier of the session to remove.</param>
	/// <returns>A task that represents the asynchronous operation. Returns true if the session was found and removed, false otherwise.</returns>
	Task<bool> RemoveSessionAsync(string sessionId);

	/// <summary>
	/// Updates a chat session's metadata (name and description) in both the database and cache.
	/// </summary>
	/// <param name="sessionId">The unique identifier of the session to update.</param>
	/// <param name="name">The new name for the session. If null, the name will not be updated.</param>
	/// <param name="description">The new description for the session. If null, the description will not be updated.</param>
	/// <returns>A task that represents the asynchronous operation. Returns true if the session was found and updated, false otherwise.</returns>
	Task<bool> UpdateSessionAsync(string sessionId, string? name = null, string? description = null);
}
