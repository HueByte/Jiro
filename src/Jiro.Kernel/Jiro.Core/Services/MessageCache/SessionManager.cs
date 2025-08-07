using Jiro.Core.IRepositories;
using Jiro.Core.Models;
using Jiro.Core.Services.Context;
using Jiro.Core.Services.Conversation.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using OpenAI.Chat;

namespace Jiro.Core.Services.MessageCache;

/// <summary>
/// Manages chat session operations with caching for performance optimization.
/// </summary>
public class SessionManager : ISessionManager
{
	private readonly ILogger<SessionManager> _logger;
	private readonly IMemoryCache _memoryCache;
	private readonly IChatSessionRepository _chatSessionRepository;
	private readonly IInstanceMetadataAccessor _instanceMetadataAccessor;
	private const int MEMORY_CACHE_EXPIRATION_DAYS = 5;

	/// <summary>
	/// Initializes a new instance of the SessionManager class.
	/// </summary>
	/// <param name="logger">The logger instance for this service.</param>
	/// <param name="memoryCache">The memory cache for session caching.</param>
	/// <param name="chatSessionRepository">The repository for session data access.</param>
	/// <param name="instanceMetadataAccessor">The service for accessing instance metadata.</param>
	/// <exception cref="ArgumentNullException">Thrown when instanceMetadataAccessor is null.</exception>
	public SessionManager(
		ILogger<SessionManager> logger,
		IMemoryCache memoryCache,
		IChatSessionRepository chatSessionRepository,
		IInstanceMetadataAccessor instanceMetadataAccessor)
	{
		_logger = logger;
		_memoryCache = memoryCache;
		_chatSessionRepository = chatSessionRepository;
		_instanceMetadataAccessor = instanceMetadataAccessor ?? throw new ArgumentNullException(nameof(instanceMetadataAccessor));
	}

	/// <inheritdoc />
	public async Task<Session?> GetSessionAsync(string sessionId, bool includeMessages = false, string? instanceId = null)
	{
		_logger.LogInformation("GetSessionAsync called with sessionId: '{SessionId}' (IncludeMessages: {IncludeMessages})", sessionId, includeMessages);

		string cacheKey = $"{Constants.CacheKeys.SessionKey}::{sessionId}";
		string? resolvedInstanceId = instanceId;

		// Try cache first
		if (_memoryCache.TryGetValue(cacheKey, out Session? cachedSession) && cachedSession != null)
		{
			_logger.LogInformation("Session found in cache with sessionId: '{SessionId}'", sessionId);

			// Return cached session if we have messages or don't need them
			if (cachedSession.Messages.Any() || !includeMessages)
			{
				return CreateImmutableCopy(cachedSession);
			}

			// Need to fetch messages for cached session without them
			if (!cachedSession.Messages.Any() && includeMessages)
			{
				_logger.LogInformation("Cached session has no messages, fetching with messages for sessionId: '{SessionId}'", sessionId);
				resolvedInstanceId ??= await _instanceMetadataAccessor.GetInstanceIdAsync("");
				if (resolvedInstanceId is null) return CreateImmutableCopy(cachedSession);

				return await FetchSessionFromDatabase(sessionId, resolvedInstanceId, true, cacheKey);
			}
		}

		// Not in cache, fetch from database
		resolvedInstanceId ??= await _instanceMetadataAccessor.GetInstanceIdAsync("");
		if (resolvedInstanceId is null) return null;

		return await FetchSessionFromDatabase(sessionId, resolvedInstanceId, includeMessages, cacheKey);
	}

	/// <inheritdoc />
	public async Task<Session> GetOrCreateChatSessionAsync(string sessionId, bool includeMessages = false)
	{
		try
		{
			string cacheKey = $"{Constants.CacheKeys.SessionKey}::{sessionId}";

			// Check cache first
			if (_memoryCache.TryGetValue(cacheKey, out Session? cachedSession) && cachedSession != null)
			{
				// Return if we have messages or don't need them
				if (cachedSession.Messages.Any() || !includeMessages)
				{
					_logger.LogInformation("Returning cached session with sessionId: '{SessionId}' (IncludeMessages: {IncludeMessages})", sessionId, includeMessages);
					return CreateImmutableCopy(cachedSession);
				}

				// Fetch messages for cached session without them
				if (!cachedSession.Messages.Any() && includeMessages)
				{
					_logger.LogInformation("Cached session has no messages, fetching with messages for sessionId: '{SessionId}'", sessionId);
					var instanceId = await _instanceMetadataAccessor.GetInstanceIdAsync("");
					if (instanceId is null) return CreateImmutableCopy(cachedSession);
					var sessionWithMessages = await FetchSessionFromDatabase(sessionId, instanceId, true, cacheKey);
					return sessionWithMessages ?? CreateImmutableCopy(cachedSession);
				}
			}

			return await _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
			{
				entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(MEMORY_CACHE_EXPIRATION_DAYS);

				var query = _chatSessionRepository.AsQueryable().Where(x => x.Id == sessionId);
				ChatSession? session;

				if (includeMessages)
				{
					session = await query.Include(x => x.Messages).FirstOrDefaultAsync();
					_logger.LogInformation("Fetched session {SessionId} from database with {MessageCount} messages", sessionId, session?.Messages?.Count ?? 0);
				}
				else
				{
					session = await query.FirstOrDefaultAsync();
				}

				string resolvedInstanceId;
				if (session is null)
				{
					resolvedInstanceId = await _instanceMetadataAccessor.GetInstanceIdAsync("") ?? throw new InvalidOperationException($"Instance ID could not be determined.");
					_logger.LogInformation("No chat session found for instance {InstanceId} and session {SessionId}. Creating new session.", resolvedInstanceId, sessionId);
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
				else
				{
					resolvedInstanceId = await _instanceMetadataAccessor.GetInstanceIdAsync("") ?? throw new InvalidOperationException($"Instance ID could not be determined.");
				}

				return CreateSessionFromEntity(session, resolvedInstanceId, includeMessages);
			}) ?? throw new InvalidOperationException($"Chat session with ID {sessionId} could not be created or retrieved.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching or caching chat session for sessionId: {SessionId}", sessionId);
			throw;
		}
	}

	/// <inheritdoc />
	public async Task<List<ChatSession>> GetSessionsAsync(string instanceId)
	{
		try
		{
			string sessionsListCacheKey = $"{Constants.CacheKeys.SessionsKey}::{instanceId}";

			return await _memoryCache.GetOrCreateAsync(sessionsListCacheKey, async entry =>
			{
				entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(MEMORY_CACHE_EXPIRATION_DAYS);

				// Get all sessions - the instanceId filtering should be done elsewhere if needed
				var sessions = await _chatSessionRepository.AsQueryable()
					.Select(x => new ChatSession
					{
						Id = x.Id,
						Name = x.Name,
						Description = x.Description,
						CreatedAt = x.CreatedAt,
						LastUpdatedAt = x.LastUpdatedAt,
						Messages = new List<Message>() // Empty for performance
					})
					.OrderByDescending(x => x.LastUpdatedAt)
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

	/// <inheritdoc />
	public async Task<bool> UpdateSessionAsync(string sessionId, string? name = null, string? description = null)
	{
		try
		{
			var instanceId = await _instanceMetadataAccessor.GetInstanceIdAsync("") ?? throw new InvalidOperationException($"Instance ID could not be determined.");

			var dbSession = await _chatSessionRepository.GetAsync(sessionId);
			if (dbSession == null)
			{
				_logger.LogWarning("Session {SessionId} not found in database", sessionId);
				return false;
			}

			bool wasUpdated = false;
			if (name is not null && dbSession.Name != name)
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

				// Invalidate caches
				InvalidateSessionCache(sessionId);
				InvalidateInstanceSessionsCache(instanceId);

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

	/// <inheritdoc />
	public async Task<bool> RemoveSessionAsync(string sessionId)
	{
		try
		{
			var instanceId = await _instanceMetadataAccessor.GetInstanceIdAsync("") ?? throw new InvalidOperationException($"Instance ID could not be determined.");

			var dbSession = await _chatSessionRepository.GetAsync(sessionId);
			if (dbSession == null)
			{
				_logger.LogWarning("Session {SessionId} not found in database", sessionId);
				return false;
			}

			await _chatSessionRepository.RemoveAsync(dbSession);
			await _chatSessionRepository.SaveChangesAsync();

			// Clear caches
			InvalidateSessionCache(sessionId);
			InvalidateInstanceSessionsCache(instanceId);

			_logger.LogInformation("Successfully removed session {SessionId} from database and cache", sessionId);
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error removing session {SessionId}", sessionId);
			throw;
		}
	}

	/// <inheritdoc />
	public void InvalidateSessionCache(string sessionId)
	{
		string cacheKey = $"{Constants.CacheKeys.SessionKey}::{sessionId}";
		_memoryCache.Remove(cacheKey);
		_logger.LogInformation("Invalidated cache for session {SessionId}", sessionId);
	}

	/// <inheritdoc />
	public void InvalidateInstanceSessionsCache(string instanceId)
	{
		string sessionsListCacheKey = $"{Constants.CacheKeys.SessionsKey}::{instanceId}";
		_memoryCache.Remove(sessionsListCacheKey);
		_logger.LogInformation("Invalidated sessions list cache for instance {InstanceId}", instanceId);
	}

	/// <summary>
	/// Fetches session from database and caches it.
	/// </summary>
	/// <param name="sessionId">The session identifier.</param>
	/// <param name="instanceId">The instance identifier.</param>
	/// <param name="includeMessages">Whether to include messages.</param>
	/// <param name="cacheKey">The cache key for storing the session.</param>
	/// <returns>The session or null if not found.</returns>
	private async Task<Session?> FetchSessionFromDatabase(string sessionId, string instanceId, bool includeMessages, string cacheKey)
	{
		var query = _chatSessionRepository.AsQueryable().Where(x => x.Id == sessionId);

		ChatSession? session;
		if (includeMessages)
		{
			session = await query
				.Include(x => x.Messages)
				.FirstOrDefaultAsync();
		}
		else
		{
			session = await query.FirstOrDefaultAsync();
		}

		if (session != null)
		{
			var result = CreateSessionFromEntity(session, instanceId, includeMessages);
			_memoryCache.Set(cacheKey, result, TimeSpan.FromDays(MEMORY_CACHE_EXPIRATION_DAYS));
			_logger.LogInformation("Session fetched from database and cached with sessionId: '{SessionId}' (IncludeMessages: {IncludeMessages}, MessageCount: {MessageCount})",
				sessionId, includeMessages, result.Messages.Count);
			return CreateImmutableCopy(result);
		}
		else
		{
			_logger.LogWarning("Session not found in database with sessionId: '{SessionId}'", sessionId);
		}

		return null;
	}

	/// <summary>
	/// Creates a Session model from a ChatSession entity.
	/// </summary>
	/// <param name="session">The chat session entity.</param>
	/// <param name="instanceId">The instance identifier.</param>
	/// <param name="includeMessages">Whether to include messages.</param>
	/// <returns>A Session model with appropriate message data.</returns>
	private static Session CreateSessionFromEntity(ChatSession session, string instanceId, bool includeMessages)
	{
		return new Session
		{
			InstanceId = instanceId,
			SessionId = session.Id,
			Name = session.Name,
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
	}

	/// <summary>
	/// Creates an immutable copy of a session to prevent cache corruption.
	/// </summary>
	/// <param name="original">The original session to copy.</param>
	/// <returns>A new session instance with copied data.</returns>
	private static Session CreateImmutableCopy(Session original)
	{
		return new Session
		{
			InstanceId = original.InstanceId,
			SessionId = original.SessionId,
			Name = original.Name,
			CreatedAt = original.CreatedAt,
			LastUpdatedAt = original.LastUpdatedAt,
			Messages = new List<ChatMessageWithMetadata>(original.Messages)
		};
	}
}
