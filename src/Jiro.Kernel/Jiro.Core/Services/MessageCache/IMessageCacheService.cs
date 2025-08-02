using Jiro.Core.Models;
using Jiro.Core.Services.Conversation.Models;

namespace Jiro.Core.Services.MessageCache;

/// <summary>
/// Manages message caching, history optimization, and message exchange operations.
/// </summary>
public interface IMessageCacheService
{
	/// <summary>
	/// Adds a chat exchange containing multiple messages to a session.
	/// </summary>
	/// <param name="sessionId">The session identifier.</param>
	/// <param name="messages">Messages with metadata for cache.</param>
	/// <param name="modelMessages">Messages for database persistence.</param>
	Task AddChatExchangeAsync(string sessionId, List<ChatMessageWithMetadata> messages, List<Message> modelMessages);

	/// <summary>
	/// Removes old messages from cache within specified range.
	/// </summary>
	/// <param name="sessionId">The session identifier.</param>
	/// <param name="range">Number of messages to keep.</param>
	void TrimMessagesInCache(string sessionId, int range);

	/// <summary>
	/// Gets message count for a session from cache.
	/// </summary>
	/// <param name="sessionId">The session identifier.</param>
	/// <returns>Number of cached messages.</returns>
	int GetCachedMessageCount(string sessionId);

	/// <summary>
	/// Updates cached session with new messages.
	/// </summary>
	/// <param name="sessionId">The session identifier.</param>
	/// <param name="messages">Messages to add to cache.</param>
	void UpdateSessionCache(string sessionId, List<ChatMessageWithMetadata> messages);

	/// <summary>
	/// Retrieves the core persona message.
	/// </summary>
	/// <returns>The persona message or null if not found.</returns>
	Task<string?> GetPersonaCoreMessageAsync();

	/// <summary>
	/// Legacy methods for backward compatibility.
	/// </summary>
	void ClearMessageCache();
	void ModifyMessage(string key, string message, int minutes);
}