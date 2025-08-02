using Jiro.Core.Models;
using Jiro.Core.Services.Conversation.Models;

namespace Jiro.Core.Services.MessageCache;

/// <summary>
/// Manages chat session operations including creation, retrieval, updates, and deletion.
/// </summary>
public interface ISessionManager
{
	/// <summary>
	/// Retrieves a session by ID with optional message loading.
	/// </summary>
	/// <param name="sessionId">The session identifier.</param>
	/// <param name="includeMessages">Whether to include messages in the result.</param>
	/// <param name="instanceId">The instance ID. If null, will be resolved automatically.</param>
	/// <returns>The session or null if not found.</returns>
	Task<Session?> GetSessionAsync(string sessionId, bool includeMessages = false, string? instanceId = null);

	/// <summary>
	/// Gets or creates a chat session.
	/// </summary>
	/// <param name="sessionId">The session identifier.</param>
	/// <param name="includeMessages">Whether to include messages in the result.</param>
	/// <returns>The session with or without messages.</returns>
	Task<Session> GetOrCreateChatSessionAsync(string sessionId, bool includeMessages = false);

	/// <summary>
	/// Retrieves all chat sessions for an instance.
	/// </summary>
	/// <param name="instanceId">The instance identifier.</param>
	/// <returns>List of chat sessions without messages for performance.</returns>
	Task<List<ChatSession>> GetSessionsAsync(string instanceId);

	/// <summary>
	/// Updates session metadata.
	/// </summary>
	/// <param name="sessionId">The session identifier.</param>
	/// <param name="name">New session name.</param>
	/// <param name="description">New session description.</param>
	/// <returns>True if updated successfully.</returns>
	Task<bool> UpdateSessionAsync(string sessionId, string? name = null, string? description = null);

	/// <summary>
	/// Removes a session and all its messages.
	/// </summary>
	/// <param name="sessionId">The session identifier.</param>
	/// <returns>True if removed successfully.</returns>
	Task<bool> RemoveSessionAsync(string sessionId);

	/// <summary>
	/// Invalidates cached session data.
	/// </summary>
	/// <param name="sessionId">The session identifier.</param>
	void InvalidateSessionCache(string sessionId);

	/// <summary>
	/// Invalidates all sessions cache for an instance.
	/// </summary>
	/// <param name="instanceId">The instance identifier.</param>
	void InvalidateInstanceSessionsCache(string instanceId);
}
