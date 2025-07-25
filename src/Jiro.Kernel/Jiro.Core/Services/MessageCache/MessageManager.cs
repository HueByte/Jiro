using Jiro.Core.IRepositories;
using Jiro.Core.Models;
using Jiro.Core.Services.CommandContext;
using Jiro.Core.Services.Conversation.Models;
using Jiro.Core.Services.StaticMessage;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using OpenAI.Chat;

namespace Jiro.Core.Services.MessageCache;

/// <summary>
/// Manages chat messages, sessions, and message caching operations.
/// </summary>
public class MessageManager : IMessageManager
{
	private readonly ILogger<MessageManager> _logger;
	private readonly IMemoryCache _memoryCache;
	private readonly IMessageRepository _messageRepository;
	private readonly IChatSessionRepository _chatSessionRepository;
	private readonly ICommandContext _commandContext;
	private readonly IConfiguration _configuration;
	private readonly IStaticMessageService _staticMessageService;
	private const int MEMORY_CACHE_EXPIRATION = 5;

	/// <summary>
	/// Initializes a new instance of the MessageManager class.
	/// </summary>
	/// <param name="logger">The logger instance.</param>
	/// <param name="memoryCache">The memory cache instance.</param>
	/// <param name="messageRepository">The message repository.</param>
	/// <param name="chatSessionRepository">The chat session repository.</param>
	/// <param name="configuration">The configuration instance.</param>
	/// <param name="commandContext">The command context.</param>
	/// <param name="staticMessageService">The static message service.</param>
	public MessageManager(ILogger<MessageManager> logger, IMemoryCache memoryCache, IMessageRepository messageRepository, IChatSessionRepository chatSessionRepository, IConfiguration configuration, ICommandContext commandContext, IStaticMessageService staticMessageService)
	{
		_logger = logger;
		_memoryCache = memoryCache;
		_messageRepository = messageRepository;
		_chatSessionRepository = chatSessionRepository;
		_configuration = configuration;
		_commandContext = commandContext ?? throw new ArgumentNullException(nameof(commandContext), "Command context cannot be null.");
		_staticMessageService = staticMessageService ?? throw new ArgumentNullException(nameof(staticMessageService), "Static message service cannot be null.");
	}

	/// <summary>
	/// Retrieves a session with optional message loading for performance optimization.
	/// </summary>
	/// <param name="sessionId">The session identifier.</param>
	/// <param name="includeMessages">Whether to include messages in the result. Defaults to false for performance.</param>
	/// <returns>The session or null if not found.</returns>
	public async Task<Session?> GetSessionAsync(string sessionId, bool includeMessages = false)
	{
		_logger.LogInformation("GetSessionAsync called with sessionId: '{SessionId}' (IncludeMessages: {IncludeMessages})", sessionId, includeMessages);

		string cacheKey = $"{Constants.CacheKeys.SessionKey}::{sessionId}";

		// Try to get session from cache first
		if (_memoryCache.TryGetValue(cacheKey, out Session? cachedSession) && cachedSession != null)
		{
			_logger.LogInformation("Session found in cache with sessionId: '{SessionId}'", sessionId);

			// If cached session has messages but caller wants them, return cached session
			if (cachedSession.Messages.Any() || !includeMessages)
			{
				return cachedSession;
			}
			// If cached session has no messages but caller wants them, we need to fetch messages
			if (!cachedSession.Messages.Any() && includeMessages)
			{
				_logger.LogInformation("Cached session has no messages, fetching with messages for sessionId: '{SessionId}'", sessionId);
				return await FetchSessionFromDatabase(sessionId, true, cacheKey);
			}
		}

		// Session not in cache, fetch from database
		return await FetchSessionFromDatabase(sessionId, includeMessages, cacheKey);
	}

	/// <summary>
	/// Fetches session from database and caches it.
	/// </summary>
	private async Task<Session?> FetchSessionFromDatabase(string sessionId, bool includeMessages, string cacheKey)
	{
		var query = _chatSessionRepository.AsQueryable().Where(x => x.Id == sessionId);

		Session? session;
		if (includeMessages)
		{
			session = await query
				.Include(x => x.Messages)
				.Select(x => new Session
				{
					InstanceId = _commandContext.InstanceId,
					SessionId = x.Id,
					CreatedAt = x.CreatedAt,
					LastUpdatedAt = x.LastUpdatedAt,
					Messages = x.Messages != null ? x.Messages.OrderBy(m => m.CreatedAt).Select(m => new ChatMessageWithMetadata()
					{
						MessageId = m.Id,
						IsUser = m.IsUser,
						CreatedAt = m.CreatedAt,
						Type = m.Type,
						Message = m.IsUser
								? ChatMessage.CreateUserMessage(m.Content)
								: ChatMessage.CreateAssistantMessage(m.Content)
					}).ToList() : new List<ChatMessageWithMetadata>()
				})
				.FirstOrDefaultAsync();
		}
		else
		{
			session = await query
				.Select(x => new Session
				{
					InstanceId = _commandContext.InstanceId,
					SessionId = x.Id,
					CreatedAt = x.CreatedAt,
					LastUpdatedAt = x.LastUpdatedAt,
					Messages = new List<ChatMessageWithMetadata>()
				})
				.FirstOrDefaultAsync();
		}

		if (session != null)
		{
			_memoryCache.Set(cacheKey, session, TimeSpan.FromDays(MEMORY_CACHE_EXPIRATION));
			_logger.LogInformation("Session fetched from database and cached with sessionId: '{SessionId}' (IncludeMessages: {IncludeMessages})", sessionId, includeMessages);
		}
		else
		{
			_logger.LogWarning("Session not found in database with sessionId: '{SessionId}'", sessionId);
		}

		return session;
	}

	/// <summary>
	/// Retrieves the core persona message used for AI interactions.
	/// </summary>
	/// <returns>A task that represents the asynchronous operation. The task result contains the persona message or null if not found.</returns>
	public async Task<string?> GetPersonaCoreMessageAsync()
	{
		return await _staticMessageService.GetPersonaCoreMessageAsync();
	}

	/// <summary>
	/// Retrieves all chat sessions associated with the specified instance without loading messages for performance.
	/// </summary>
	/// <param name="instanceId">The unique identifier of the instance.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains a list of chat sessions.</returns>
	public async Task<List<ChatSession>> GetChatSessionsAsync(string instanceId)
	{
		try
		{
			string sessionsListCacheKey = $"{Constants.CacheKeys.SessionsKey}::{instanceId}";

			return await _memoryCache.GetOrCreateAsync(sessionsListCacheKey, async entry =>
			{
				entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(MEMORY_CACHE_EXPIRATION);

				var sessions = await _chatSessionRepository.AsQueryable()
					.Where(x => x.Messages.Any(m => m.InstanceId == instanceId) || !x.Messages.Any())
					.Select(x => new ChatSession
					{
						Id = x.Id,
						Name = x.Name,
						Description = x.Description,
						CreatedAt = x.CreatedAt,
						LastUpdatedAt = x.LastUpdatedAt,
						Messages = new List<Message>() // Empty list for performance
					})
					.OrderByDescending(x => x.LastUpdatedAt) // Most recent sessions first
					.ToListAsync();

				if (sessions == null || !sessions.Any())
				{
					_logger.LogInformation("No chat sessions found for instance {InstanceId}", instanceId);
					return new List<ChatSession>();
				}

				_logger.LogInformation("Retrieved {Count} chat sessions for instance {InstanceId}", sessions.Count, instanceId);
				return sessions;
			}) ?? throw new InvalidOperationException($"Chat sessions for instance {instanceId} could not be retrieved.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving chat sessions for instance {InstanceId}", instanceId);
			throw;
		}
	}

	/// <summary>
	/// Gets or creates a chat session, optionally including messages for performance optimization.
	/// </summary>
	/// <param name="sessionId">The session identifier.</param>
	/// <param name="includeMessages">Whether to include messages in the result. Defaults to false for performance.</param>
	/// <returns>The session with or without messages based on the includeMessages parameter.</returns>
	public async Task<Session> GetOrCreateChatSessionAsync(string sessionId, bool includeMessages = false)
	{
		try
		{
			string cacheKey = $"{Constants.CacheKeys.SessionKey}::{sessionId}";

			// Check if session exists in cache
			if (_memoryCache.TryGetValue(cacheKey, out Session? cachedSession) && cachedSession != null)
			{
				// If we have messages in cache or caller doesn't want messages, return cached session
				if (cachedSession.Messages.Any() || !includeMessages)
				{
					_logger.LogInformation("Returning cached session with sessionId: '{SessionId}' (IncludeMessages: {IncludeMessages})", sessionId, includeMessages);
					return cachedSession;
				}

				// If cached session has no messages but caller wants them, fetch messages and update cache
				if (!cachedSession.Messages.Any() && includeMessages)
				{
					_logger.LogInformation("Cached session has no messages, fetching with messages for sessionId: '{SessionId}'", sessionId);
					var sessionWithMessages = await FetchSessionFromDatabase(sessionId, true, cacheKey);
					return sessionWithMessages ?? cachedSession; // Fallback to cached if fetch fails
				}
			}

			return await _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
			{
				entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(MEMORY_CACHE_EXPIRATION);

				var query = _chatSessionRepository.AsQueryable().Where(x => x.Id == sessionId);
				ChatSession? session;

				if (includeMessages)
				{
					session = await query.Include(x => x.Messages).FirstOrDefaultAsync();
				}
				else
				{
					session = await query.FirstOrDefaultAsync();
				}

				if (session is null)
				{
					_logger.LogInformation("No chat session found for instance {InstanceId} and session {SessionId}. Creating new session.", _commandContext.InstanceId, sessionId);
					session = new ChatSession
					{
						Id = sessionId,
						Name = $"Session-{sessionId}",
						CreatedAt = DateTime.UtcNow,
						LastUpdatedAt = DateTime.UtcNow,
						Messages = new List<Message>()
					};

					await _chatSessionRepository.AddAsync(session);
					await _chatSessionRepository.SaveChangesAsync();
				}

				return new Session
				{
					InstanceId = _commandContext.InstanceId,
					SessionId = session.Id,
					CreatedAt = session.CreatedAt,
					LastUpdatedAt = session.LastUpdatedAt,
					Messages = includeMessages && session.Messages != null ?
						session.Messages.OrderBy(x => x.CreatedAt).Select(x => new ChatMessageWithMetadata()
						{
							MessageId = x.Id,
							IsUser = x.IsUser,
							CreatedAt = x.CreatedAt,
							Type = x.Type,
							Message = x.IsUser
									? ChatMessage.CreateUserMessage(x.Content)
									: ChatMessage.CreateAssistantMessage(x.Content)
						}).ToList() : new List<ChatMessageWithMetadata>()
				};
			}) ?? throw new InvalidOperationException($"Chat session with ID {sessionId} could not be created or retrieved.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching or caching chat messages for instance {InstanceId}", _commandContext.InstanceId);
			throw;
		}
	}

	/// <summary>
	/// Adds a new chat exchange containing multiple messages to the specified session.
	/// </summary>
	/// <param name="sessionId">The unique identifier of the session.</param>
	/// <param name="messages">The list of chat messages with metadata to add.</param>
	/// <param name="modelMessages">The list of model messages to add to the database.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	public async Task AddChatExchangeAsync(string sessionId, List<ChatMessageWithMetadata> messages, List<Message> modelMessages)
	{
		try
		{
			var instanceId = _commandContext.InstanceId ?? throw new InvalidOperationException("Command context or current instance is not set.");

			// Ensure all message IDs are unique by checking against database and regenerating any conflicts upfront
			var existingMessageIds = await _messageRepository.AsQueryable()
				.Where(m => modelMessages.Select(mm => mm.Id).Contains(m.Id))
				.Select(m => m.Id)
				.ToListAsync();

			foreach (var message in modelMessages.Where(m => existingMessageIds.Contains(m.Id)))
			{
				var oldId = message.Id;
				message.Id = Guid.NewGuid().ToString();
				_logger.LogWarning("Regenerated message ID from {OldId} to {NewId} to avoid conflict", oldId, message.Id);

				// Update corresponding ChatMessageWithMetadata
				var correspondingMessage = messages.FirstOrDefault(m => m.MessageId == oldId);
				if (correspondingMessage != null)
				{
					correspondingMessage.MessageId = message.Id;
				}
			}

			// Update session timestamp in database
			var dbSession = await _chatSessionRepository.GetAsync(sessionId);
			if (dbSession != null)
			{
				dbSession.LastUpdatedAt = DateTime.UtcNow;
				await _chatSessionRepository.UpdateAsync(dbSession);
			}

			// Persist messages to database
			await _messageRepository.AddRangeAsync(modelMessages);
			await _messageRepository.SaveChangesAsync();

			// Update cached session with messages if it exists
			string cacheKey = $"{Constants.CacheKeys.SessionKey}::{sessionId}";
			if (_memoryCache.TryGetValue(cacheKey, out Session? sessionWithMessages) && sessionWithMessages != null)
			{
				sessionWithMessages.Messages.AddRange(messages);
				sessionWithMessages.LastUpdatedAt = DateTime.UtcNow;
				_memoryCache.Set(cacheKey, sessionWithMessages, TimeSpan.FromDays(MEMORY_CACHE_EXPIRATION));

				_logger.LogInformation("Updated cached session with {MessageCount} new messages for session {SessionId}", messages.Count, sessionId);
			}
			else
			{
				_logger.LogInformation("Session not in cache, messages persisted to database only for session {SessionId}", sessionId);
			}

			// Invalidate sessions list cache for this instance to ensure fresh data
			var sessionsListCacheKey = $"{Constants.CacheKeys.SessionsKey}::{instanceId}";
			_memoryCache.Remove(sessionsListCacheKey);

			_logger.LogInformation("Added {MessageCount} messages to session {SessionId}", messages.Count, sessionId);
		}
		catch (Exception ex)
		{
			_logger?.LogError(ex, "Error adding chat exchange for session {SessionId}", sessionId);
			throw;
		}
	}

	/// <summary>
	/// Removes old messages from the specified session within the given range from cache.
	/// This method is typically called by the history optimizer to manage token limits.
	/// </summary>
	/// <param name="sessionId">The unique identifier of the session.</param>
	/// <param name="range">The number of messages to keep (removes messages beyond this count).</param>
	public void ClearOldMessages(string sessionId, int range)
	{
		string cacheKey = $"{Constants.CacheKeys.SessionKey}::{sessionId}";

		if (_memoryCache.TryGetValue(cacheKey, out Session? session) && session != null && session.Messages.Count > range)
		{
			int messagesToRemove = session.Messages.Count - range;
			_logger.LogInformation("Clearing {MessageCount} old messages from session {SessionId}, keeping {Range} messages",
				messagesToRemove, sessionId, range);

			session.Messages.RemoveRange(0, messagesToRemove);
			session.LastUpdatedAt = DateTime.UtcNow;
			_memoryCache.Set(cacheKey, session, TimeSpan.FromDays(MEMORY_CACHE_EXPIRATION));

			_logger.LogInformation("Successfully cleared {MessageCount} old messages from session {SessionId}", messagesToRemove, sessionId);
		}
		else
		{
			_logger.LogInformation("No messages to clear for session {SessionId} (not in cache or message count <= range)", sessionId);
		}
	}

	/// <summary>
	/// Clears all cached data for a specific session.
	/// Useful when the session needs to be completely refreshed from the database.
	/// </summary>
	/// <param name="sessionId">The unique identifier of the session.</param>
	public void InvalidateSessionCache(string sessionId)
	{
		string cacheKey = $"{Constants.CacheKeys.SessionKey}::{sessionId}";
		_memoryCache.Remove(cacheKey);
		_logger.LogInformation("Invalidated cache for session {SessionId}", sessionId);
	}

	/// <summary>
	/// Clears all cached sessions for a specific instance.
	/// Useful when a complete cache refresh is needed.
	/// </summary>
	/// <param name="instanceId">The unique identifier of the instance.</param>
	public void InvalidateInstanceSessionsCache(string instanceId)
	{
		string sessionsListCacheKey = $"{Constants.CacheKeys.SessionsKey}::{instanceId}";
		_memoryCache.Remove(sessionsListCacheKey);
		_logger.LogInformation("Invalidated sessions list cache for instance {InstanceId}", instanceId);
	}

	/// <summary>
	/// Clears all message cache entries. Legacy method for compatibility.
	/// </summary>
	public void ClearMessageCache()
	{
		// Delegate static message cache clearing to the dedicated service
		_staticMessageService.ClearStaticMessageCache();

		_logger.LogInformation("Cleared message cache entries");
	}

	/// <summary>
	/// Modifies a message in the cache with the specified key and expiration. Legacy method for compatibility.
	/// </summary>
	/// <param name="key">The cache key.</param>
	/// <param name="message">The message content.</param>
	/// <param name="minutes">The expiration time in minutes.</param>
	public void ModifyMessage(string key, string message, int minutes)
	{
		// Delegate to static message service for static messages
		_staticMessageService.SetStaticMessage(key, message, minutes);
		_logger.LogInformation("Modified message with key {Key} and expiration {Minutes} minutes", key, minutes);
	}

	/// <summary>
	/// Gets the count of messages in a specific session from cache. Legacy method for compatibility.
	/// </summary>
	/// <param name="sessionId">The session identifier.</param>
	/// <returns>The number of messages in the session.</returns>
	public int GetChatMessageCount(string sessionId)
	{
		string cacheKey = $"{Constants.CacheKeys.SessionKey}::{sessionId}";

		if (_memoryCache.TryGetValue(cacheKey, out Session? session) && session != null)
		{
			return session.Messages.Count;
		}

		// Also check the old cache key format for backward compatibility
		string legacyCacheKey = $"{sessionId}_with_messages";
		if (_memoryCache.TryGetValue(legacyCacheKey, out Session? legacySession) && legacySession != null)
		{
			return legacySession.Messages.Count;
		}

		return 0;
	}

	/// <summary>
	/// Removes a chat session and all its messages from both the database and cache.
	/// </summary>
	/// <param name="sessionId">The unique identifier of the session to remove.</param>
	/// <returns>A task that represents the asynchronous operation. Returns true if the session was found and removed, false otherwise.</returns>
	public async Task<bool> RemoveSessionAsync(string sessionId)
	{
		try
		{
			var instanceId = _commandContext.InstanceId ?? throw new InvalidOperationException("Command context or current instance is not set.");

			// Remove session from database
			var dbSession = await _chatSessionRepository.GetAsync(sessionId);
			if (dbSession == null)
			{
				_logger.LogWarning("Session {SessionId} not found in database", sessionId);
				return false;
			}

			// Remove the session (messages will be cascade deleted automatically)
			await _chatSessionRepository.RemoveAsync(dbSession);
			await _chatSessionRepository.SaveChangesAsync();

			// Remove from cache
			string cacheKey = $"{Constants.CacheKeys.SessionKey}::{sessionId}";
			_memoryCache.Remove(cacheKey);

			// Invalidate sessions list cache for this instance
			string sessionsListCacheKey = $"{Constants.CacheKeys.SessionsKey}::{instanceId}";
			_memoryCache.Remove(sessionsListCacheKey);

			_logger.LogInformation("Successfully removed session {SessionId} from database and cache", sessionId);
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error removing session {SessionId}", sessionId);
			throw;
		}
	}

	/// <summary>
	/// Updates a chat session's metadata (name and description) in both the database and cache.
	/// </summary>
	/// <param name="sessionId">The unique identifier of the session to update.</param>
	/// <param name="name">The new name for the session. If null, the name will not be updated.</param>
	/// <param name="description">The new description for the session. If null, the description will not be updated.</param>
	/// <returns>A task that represents the asynchronous operation. Returns true if the session was found and updated, false otherwise.</returns>
	public async Task<bool> UpdateSessionAsync(string sessionId, string? name = null, string? description = null)
	{
		try
		{
			var instanceId = _commandContext.InstanceId ?? throw new InvalidOperationException("Command context or current instance is not set.");

			// Update session in database
			var dbSession = await _chatSessionRepository.GetAsync(sessionId);
			if (dbSession == null)
			{
				_logger.LogWarning("Session {SessionId} not found in database", sessionId);
				return false;
			}

			bool wasUpdated = false;
			if (name != null && dbSession.Name != name)
			{
				dbSession.Name = name;
				wasUpdated = true;
			}

			if (description != null && dbSession.Description != description)
			{
				dbSession.Description = description;
				wasUpdated = true;
			}

			if (wasUpdated)
			{
				dbSession.LastUpdatedAt = DateTime.UtcNow;
				await _chatSessionRepository.UpdateAsync(dbSession);
				await _chatSessionRepository.SaveChangesAsync();

				// Update cached session if it exists
				string cacheKey = $"{Constants.CacheKeys.SessionKey}::{sessionId}";
				if (_memoryCache.TryGetValue(cacheKey, out Session? cachedSession) && cachedSession != null)
				{
					cachedSession.LastUpdatedAt = dbSession.LastUpdatedAt;
					_memoryCache.Set(cacheKey, cachedSession, TimeSpan.FromDays(MEMORY_CACHE_EXPIRATION));
				}

				// Invalidate sessions list cache to reflect updated metadata
				string sessionsListCacheKey = $"{Constants.CacheKeys.SessionsKey}::{instanceId}";
				_memoryCache.Remove(sessionsListCacheKey);

				_logger.LogInformation("Successfully updated session {SessionId} metadata", sessionId);
			}
			else
			{
				_logger.LogInformation("No updates needed for session {SessionId} - values unchanged", sessionId);
			}

			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating session {SessionId}", sessionId);
			throw;
		}
	}
}
