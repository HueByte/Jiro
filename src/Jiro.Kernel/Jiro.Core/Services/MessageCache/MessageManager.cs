using Jiro.Core.IRepositories;
using Jiro.Core.Models;
using Jiro.Core.Services.CommandContext;
using Jiro.Core.Services.Conversation.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using OpenAI.Chat;

namespace Jiro.Core.Services.MessageCache;

public class MessageManager : IMessageManager
{
	private readonly ILogger<MessageManager> _logger;
	private readonly IMemoryCache _memoryCache;
	private readonly IMessageRepository _messageRepository;
	private readonly IChatSessionRepository _chatSessionRepository;
	private readonly ICommandContext _commandContext;
	private readonly int _messageFetchCount = 40;
	private const int MEMORY_CACHE_EXPIRATION = 5;

	public MessageManager(ILogger<MessageManager> logger, IMemoryCache memoryCache, IMessageRepository messageRepository, IChatSessionRepository chatSessionRepository, IConfiguration configuration, ICommandContext commandContext)
	{
		_logger = logger;
		_memoryCache = memoryCache;
		_messageRepository = messageRepository;
		_chatSessionRepository = chatSessionRepository;
		_messageFetchCount = configuration.GetValue<int>(Constants.Environment.MessageFetchCount);
		_commandContext = commandContext ?? throw new ArgumentNullException(nameof(commandContext), "Command context cannot be null.");
	}

	public void ClearMessageCache()
	{
		// Log cache clear action
		_logger?.LogInformation("Clearing persona and core persona message cache.");
		_memoryCache.Remove(Constants.CacheKeys.ComputedPersonaMessageKey);
		_memoryCache.Remove(Constants.CacheKeys.CorePersonaMessageKey);
	}

	public async Task<List<ChatSession>> GetChatSessionsAsync(string instanceId)
	{
		try
		{
			return await _memoryCache.GetOrCreateAsync(instanceId, async entry =>
			{
				entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(MEMORY_CACHE_EXPIRATION);

				var sessions = await FetchChatSessionsAsync(instanceId);
				if (sessions == null || !sessions.Any())
				{
					_logger.LogInformation("No chat sessions found for instance {InstanceId}", instanceId);
					return new List<ChatSession>();
				}

				_logger.LogInformation("Retrieved {Count} chat sessions for instance {InstanceId}", sessions.Count, instanceId);

				_memoryCache.Set(Constants.CacheKeys.SessionsKey, sessions, TimeSpan.FromDays(MEMORY_CACHE_EXPIRATION));
				return sessions;
			}) ?? throw new InvalidOperationException($"Chat sessions for instance {instanceId} could not be retrieved.");
		}

		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving chat sessions for instance {InstanceId}", instanceId);
			throw;
		}
	}

	public async Task<Session?> GetSessionAsync(string sessionId)
	{
		if (!_memoryCache.TryGetValue(sessionId, out Session? session))
		{
			session = await _chatSessionRepository.AsQueryable()
				.Where(x => x.Id == sessionId)
				.Include(x => x.Messages.OrderBy(m => m.CreatedAt))
				.Select(x => new Session
				{
					InstanceId = _commandContext.InstanceId,
					SessionId = x.Id,
					CreatedAt = x.CreatedAt,
					LastUpdatedAt = x.LastUpdatedAt,
					Messages = x.Messages != null ? x.Messages.Select(m => new ChatMessageWithMetadata()
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

			if (session != null)
			{
				_memoryCache.Set(sessionId, session, TimeSpan.FromDays(MEMORY_CACHE_EXPIRATION));
			}
		}

		return session;
	}

	public async Task<string?> GetPersonaCoreMessageAsync()
	{
		return await GetMessageAsync(Constants.CacheKeys.CorePersonaMessageKey);
	}

	public int GetChatMessageCount(string instanceId)
	{
		if (_memoryCache.TryGetValue(instanceId, out List<ChatMessage>? channelMessages))
		{
			return channelMessages?.Count ?? 0;
		}

		return 0;
	}

	public async Task<Session> GetOrCreateChatSessionAsync(string sessionId)
	{
		try
		{
			return await _memoryCache.GetOrCreateAsync(sessionId, async entry =>
			{
				entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(MEMORY_CACHE_EXPIRATION);
				var session = await _chatSessionRepository.AsQueryable()
					.Where(x => x.Id == sessionId)
					.Include(x => x.Messages.OrderBy(m => m.CreatedAt))
					.FirstOrDefaultAsync();

				if (session is null)
				{
					_logger.LogInformation("No chat session found for instance {InstanceId} and session {SessionId}. Creating new session.", _commandContext.InstanceId, sessionId);
					session = new ChatSession
					{
						Id = sessionId,
						Name = $"Session-{sessionId}",
						CreatedAt = DateTime.UtcNow,
						LastUpdatedAt = DateTime.UtcNow,
					};

					await _chatSessionRepository.AddAsync(session);
					await _chatSessionRepository.SaveChangesAsync();
					_memoryCache.Remove(Constants.CacheKeys.SessionsKey);
				}

				return new Session
				{
					InstanceId = _commandContext.InstanceId,
					SessionId = session.Id,
					CreatedAt = session.CreatedAt,
					LastUpdatedAt = session.LastUpdatedAt,
					Messages = session.Messages.Select(x => new ChatMessageWithMetadata()
					{
						MessageId = x.Id,
						IsUser = x.IsUser,
						CreatedAt = x.CreatedAt,
						Type = x.Type,
						Message = x.IsUser
								? ChatMessage.CreateUserMessage(x.Content)
								: ChatMessage.CreateAssistantMessage(x.Content)
					}).ToList()
				};
			}) ?? throw new InvalidOperationException($"Chat session with ID {sessionId} could not be created or retrieved.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching or caching chat messages for instance {InstanceId}", _commandContext.InstanceId);
			throw;
		}
	}

	public async Task AddChatExchangeAsync(string sessionId, List<ChatMessageWithMetadata> messages, List<Message> modelMessages)
	{
		try
		{
			var instanceId = _commandContext.InstanceId ?? throw new InvalidOperationException("Command context or current instance is not set.");

			// Get or create session first
			var session = await GetOrCreateChatSessionAsync(sessionId);

			// Update cache with new messages
			session.Messages.AddRange(messages);
			session.LastUpdatedAt = DateTime.UtcNow;
			_memoryCache.Set(sessionId, session, TimeSpan.FromDays(MEMORY_CACHE_EXPIRATION));

			// Persist to database
			await _messageRepository.AddRangeAsync(modelMessages);
			await _messageRepository.SaveChangesAsync();

			// Update the chat session in database as well
			var dbSession = await _chatSessionRepository.GetAsync(sessionId);
			if (dbSession != null)
			{
				dbSession.LastUpdatedAt = DateTime.UtcNow;
				await _chatSessionRepository.UpdateAsync(dbSession);
				await _chatSessionRepository.SaveChangesAsync();
			}

			// Invalidate sessions cache to ensure fresh data
			_memoryCache.Remove(Constants.CacheKeys.SessionsKey);

			_logger.LogInformation("Added {MessageCount} messages to session {SessionId}", messages.Count, sessionId);
		}
		catch (Exception ex)
		{
			_logger?.LogError(ex, "Error adding chat exchange for session {SessionId}", sessionId);
			throw;
		}
	}

	public void ClearOldMessages(string sessionId, int range)
	{
		if (!_memoryCache.TryGetValue(sessionId, out Session? session))
		{
			return;
		}

		if (session is not null && session.Messages.Count > range)
		{
			session.Messages.RemoveRange(0, session.Messages.Count - range);
			_memoryCache.Set(sessionId, session, TimeSpan.FromDays(MEMORY_CACHE_EXPIRATION));
		}
	}

	public void ModifyMessage(string key, string message, int minutes = 30)
	{
		_memoryCache.Set(key, message, TimeSpan.FromMinutes(minutes));
	}

	/// <summary>
	/// Validates session consistency between cache and database.
	/// </summary>
	/// <param name="sessionId">The session identifier to validate.</param>
	/// <returns>True if session is consistent, false otherwise.</returns>
	public async Task<bool> ValidateSessionConsistencyAsync(string sessionId)
	{
		try
		{
			var cacheSession = _memoryCache.Get<Session>(sessionId);
			var dbSession = await _chatSessionRepository.GetAsync(sessionId);

			if (cacheSession == null && dbSession == null)
				return true; // Both null is consistent

			if (cacheSession == null || dbSession == null)
			{
				_logger.LogWarning("Session consistency issue: Cache session: {CacheExists}, DB session: {DbExists}",
					cacheSession != null, dbSession != null);
				return false;
			}

			// Compare basic properties
			if (cacheSession.SessionId != dbSession.Id ||
				Math.Abs((cacheSession.LastUpdatedAt - dbSession.LastUpdatedAt).TotalSeconds) > 1)
			{
				_logger.LogWarning("Session {SessionId} has inconsistent data between cache and database", sessionId);
				return false;
			}

			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error validating session consistency for {SessionId}", sessionId);
			return false;
		}
	}

	/// <summary>
	/// Repairs session consistency issues by refreshing from database.
	/// </summary>
	/// <param name="sessionId">The session identifier to repair.</param>
	/// <returns>True if repair was successful.</returns>
	public async Task<bool> RepairSessionConsistencyAsync(string sessionId)
	{
		try
		{
			_memoryCache.Remove(sessionId);
			var session = await GetSessionAsync(sessionId);

			if (session != null)
			{
				_logger.LogInformation("Repaired session consistency for {SessionId}", sessionId);
				return true;
			}

			_logger.LogWarning("Could not repair session {SessionId} - session not found", sessionId);
			return false;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error repairing session consistency for {SessionId}", sessionId);
			return false;
		}
	}

	private async Task<string?> GetMessageAsync(string key)
	{
		if (_memoryCache.TryGetValue(key, out string? message))
		{
			return message;
		}
		else if (File.Exists(Path.Join(Constants.Paths.MessageBasePath, $"{key}.md")))
		{
			message = await File.ReadAllTextAsync(Path.Join(Constants.Paths.MessageBasePath, $"{key}.md"));
			_memoryCache.Set(key, message, TimeSpan.FromMinutes(5));
			return message;
		}

		return null;
	}

	private async Task<List<ChatSession>> FetchChatSessionsAsync(string instanceId)
	{
		try
		{
			// Return sessions that either:
			// 1. Have messages from this instance, OR
			// 2. Have no messages at all (empty sessions that could belong to any instance)
			var sessions = await _chatSessionRepository.AsQueryable()
				.Include(x => x.Messages.OrderBy(m => m.CreatedAt))
				.Where(x => x.Messages.Any(m => m.InstanceId == instanceId) || !x.Messages.Any())
				.ToListAsync();

			if (sessions == null || !sessions.Any())
			{
				_logger.LogInformation("No chat sessions found for instance {InstanceId}", instanceId);
				return new List<ChatSession>();
			}

			_logger.LogInformation("Found {Count} chat sessions for instance {InstanceId}", sessions.Count, instanceId);
			return sessions;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching chat sessions for instance {InstanceId}", instanceId);
			throw;
		}
	}
}
