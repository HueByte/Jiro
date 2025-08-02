using Jiro.Core.Models;
using Jiro.Core.Services.Conversation.Models;

namespace Jiro.Core.Services.MessageCache;

/// <summary>
/// Composite service that combines SessionManager and MessageCacheService functionality.
/// Provides the IMessageManager interface by delegating to the specialized services.
/// </summary>
public class CompositeMessageManager : IMessageManager
{
	private readonly ISessionManager _sessionManager;
	private readonly IMessageCacheService _messageCacheService;

	/// <summary>
	/// Initializes a new instance of the CompositeMessageManager class.
	/// </summary>
	/// <param name="sessionManager">The session management service.</param>
	/// <param name="messageCacheService">The message caching service.</param>
	/// <exception cref="ArgumentNullException">Thrown when sessionManager or messageCacheService is null.</exception>
	public CompositeMessageManager(ISessionManager sessionManager, IMessageCacheService messageCacheService)
	{
		_sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
		_messageCacheService = messageCacheService ?? throw new ArgumentNullException(nameof(messageCacheService));
	}

	/// <inheritdoc />
	public async Task<Session?> GetSessionAsync(string sessionId, bool includeMessages = false, string? instanceId = null)
	{
		return await _sessionManager.GetSessionAsync(sessionId, includeMessages, instanceId);
	}

	/// <inheritdoc />
	public async Task<Session> GetOrCreateChatSessionAsync(string sessionId, bool includeMessages = false)
	{
		return await _sessionManager.GetOrCreateChatSessionAsync(sessionId, includeMessages);
	}

	/// <inheritdoc />
	public async Task<List<ChatSession>> GetChatSessionsAsync(string instanceId)
	{
		return await _sessionManager.GetSessionsAsync(instanceId);
	}

	/// <inheritdoc />
	public async Task<bool> UpdateSessionAsync(string sessionId, string? name = null, string? description = null)
	{
		return await _sessionManager.UpdateSessionAsync(sessionId, name, description);
	}

	/// <inheritdoc />
	public async Task<bool> RemoveSessionAsync(string sessionId)
	{
		return await _sessionManager.RemoveSessionAsync(sessionId);
	}

	/// <inheritdoc />
	public void InvalidateSessionCache(string sessionId)
	{
		_sessionManager.InvalidateSessionCache(sessionId);
	}

	/// <inheritdoc />
	public void InvalidateInstanceSessionsCache(string instanceId)
	{
		_sessionManager.InvalidateInstanceSessionsCache(instanceId);
	}

	/// <inheritdoc />
	public async Task AddChatExchangeAsync(string sessionId, List<ChatMessageWithMetadata> messages, List<Message> modelMessages)
	{
		await _messageCacheService.AddChatExchangeAsync(sessionId, messages, modelMessages);
	}

	/// <inheritdoc />
	public void ClearOldMessages(string sessionId, int range)
	{
		_messageCacheService.TrimMessagesInCache(sessionId, range);
	}

	/// <inheritdoc />
	public int GetChatMessageCount(string sessionId)
	{
		return _messageCacheService.GetCachedMessageCount(sessionId);
	}

	/// <inheritdoc />
	public async Task<string?> GetPersonaCoreMessageAsync()
	{
		return await _messageCacheService.GetPersonaCoreMessageAsync();
	}

	/// <inheritdoc />
	public void ClearMessageCache()
	{
		_messageCacheService.ClearMessageCache();
	}

	/// <inheritdoc />
	public void ModifyMessage(string key, string message, int minutes)
	{
		_messageCacheService.ModifyMessage(key, message, minutes);
	}
}