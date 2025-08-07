using Jiro.Core.IRepositories;
using Jiro.Core.Models;
using Jiro.Core.Services.Context;
using Jiro.Core.Services.Conversation.Models;
using Jiro.Core.Services.StaticMessage;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Jiro.Core.Services.MessageCache;

/// <summary>
/// Handles message caching, history optimization, and message exchange operations.
/// Separated from session management for better separation of concerns.
/// </summary>
public class MessageCacheService : IMessageCacheService
{
	private readonly ILogger<MessageCacheService> _logger;
	private readonly IMemoryCache _memoryCache;
	private readonly IMessageRepository _messageRepository;
	private readonly IChatSessionRepository _chatSessionRepository;
	private readonly IStaticMessageService _staticMessageService;
	private readonly IInstanceMetadataAccessor _instanceMetadataAccessor;
	private const int MEMORY_CACHE_EXPIRATION_DAYS = 5;

	/// <summary>
	/// Initializes a new instance of the MessageCacheService class.
	/// </summary>
	/// <param name="logger">The logger instance for this service.</param>
	/// <param name="memoryCache">The memory cache for message caching.</param>
	/// <param name="messageRepository">The repository for message data access.</param>
	/// <param name="chatSessionRepository">The repository for session data access.</param>
	/// <param name="staticMessageService">The service for static message operations.</param>
	/// <param name="instanceMetadataAccessor">The service for accessing instance metadata.</param>
	/// <exception cref="ArgumentNullException">Thrown when staticMessageService or instanceMetadataAccessor is null.</exception>
	public MessageCacheService(
		ILogger<MessageCacheService> logger,
		IMemoryCache memoryCache,
		IMessageRepository messageRepository,
		IChatSessionRepository chatSessionRepository,
		IStaticMessageService staticMessageService,
		IInstanceMetadataAccessor instanceMetadataAccessor)
	{
		_logger = logger;
		_memoryCache = memoryCache;
		_messageRepository = messageRepository;
		_chatSessionRepository = chatSessionRepository;
		_staticMessageService = staticMessageService ?? throw new ArgumentNullException(nameof(staticMessageService));
		_instanceMetadataAccessor = instanceMetadataAccessor ?? throw new ArgumentNullException(nameof(instanceMetadataAccessor));
	}

	/// <inheritdoc />
	public async Task AddChatExchangeAsync(string sessionId, List<ChatMessageWithMetadata> messages, List<Message> modelMessages)
	{
		try
		{
			var instanceId = await _instanceMetadataAccessor.GetInstanceIdAsync("") ?? throw new InvalidOperationException($"Instance ID could not be determined.");

			// Ensure unique message IDs
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
			_logger.LogInformation("Saved {MessageCount} messages to database for session {SessionId}", modelMessages.Count, sessionId);

			// Update cache safely
			UpdateSessionCache(sessionId, messages);

			// Invalidate sessions list cache for this instance
			var sessionsListCacheKey = $"{Constants.CacheKeys.SessionsKey}::{instanceId}";
			_memoryCache.Remove(sessionsListCacheKey);

			_logger.LogInformation("Added {MessageCount} messages to session {SessionId} - Cache updated", messages.Count, sessionId);
		}
		catch (Exception ex)
		{
			_logger?.LogError(ex, "Error adding chat exchange for session {SessionId}", sessionId);
			throw;
		}
	}

	/// <inheritdoc />
	public void TrimMessagesInCache(string sessionId, int range)
	{
		string cacheKey = $"{Constants.CacheKeys.SessionKey}::{sessionId}";

		if (_memoryCache.TryGetValue(cacheKey, out Session? session) && session != null && session.Messages.Count > range)
		{
			int messagesToRemove = session.Messages.Count - range;
			_logger.LogInformation("Trimming {MessageCount} old messages from session {SessionId}, keeping {Range} messages",
				messagesToRemove, sessionId, range);

			// Create new session with trimmed messages to avoid mutating cached object
			var trimmedMessages = session.Messages.Skip(messagesToRemove).ToList();
			var updatedSession = new Session
			{
				InstanceId = session.InstanceId,
				SessionId = session.SessionId,
				Name = session.Name,
				CreatedAt = session.CreatedAt,
				LastUpdatedAt = DateTime.UtcNow,
				Messages = trimmedMessages
			};

			_memoryCache.Set(cacheKey, updatedSession, TimeSpan.FromDays(MEMORY_CACHE_EXPIRATION_DAYS));

			_logger.LogInformation("Successfully trimmed {MessageCount} old messages from session {SessionId}", messagesToRemove, sessionId);
		}
		else
		{
			_logger.LogInformation("No messages to trim for session {SessionId} (not in cache or message count <= range)", sessionId);
		}
	}

	/// <inheritdoc />
	public int GetCachedMessageCount(string sessionId)
	{
		string cacheKey = $"{Constants.CacheKeys.SessionKey}::{sessionId}";

		if (_memoryCache.TryGetValue(cacheKey, out Session? session) && session != null)
		{
			return session.Messages.Count;
		}

		return 0;
	}

	/// <inheritdoc />
	public void UpdateSessionCache(string sessionId, List<ChatMessageWithMetadata> messages)
	{
		string cacheKey = $"{Constants.CacheKeys.SessionKey}::{sessionId}";

		if (_memoryCache.TryGetValue(cacheKey, out Session? sessionWithMessages) && sessionWithMessages != null)
		{
			// Create new session with updated messages to avoid mutating cached object
			var updatedMessages = new List<ChatMessageWithMetadata>(sessionWithMessages.Messages);
			updatedMessages.AddRange(messages);

			var updatedSession = new Session
			{
				InstanceId = sessionWithMessages.InstanceId,
				SessionId = sessionWithMessages.SessionId,
				Name = sessionWithMessages.Name,
				CreatedAt = sessionWithMessages.CreatedAt,
				LastUpdatedAt = DateTime.UtcNow,
				Messages = updatedMessages
			};

			_memoryCache.Set(cacheKey, updatedSession, TimeSpan.FromDays(MEMORY_CACHE_EXPIRATION_DAYS));

			_logger.LogInformation("Updated cached session with {MessageCount} new messages for session {SessionId} (Total: {TotalCount})",
				messages.Count, sessionId, updatedMessages.Count);
		}
		else
		{
			// Session not in cache - remove it to force reload from database next time
			_memoryCache.Remove(cacheKey);
			_logger.LogInformation("Session not in cache, cleared cache key and messages persisted to database for session {SessionId}", sessionId);
		}
	}

	/// <inheritdoc />
	public async Task<string?> GetPersonaCoreMessageAsync()
	{
		return await _staticMessageService.GetPersonaCoreMessageAsync();
	}

	/// <inheritdoc />
	public void ClearMessageCache()
	{
		// Delegate static message cache clearing to the dedicated service
		_staticMessageService.ClearStaticMessageCache();
		_logger.LogInformation("Cleared message cache entries");
	}

	/// <inheritdoc />
	public void ModifyMessage(string key, string message, int minutes)
	{
		// Delegate to static message service for static messages
		_staticMessageService.SetStaticMessage(key, message, minutes);
		_logger.LogInformation("Modified message with key {Key} and expiration {Minutes} minutes", key, minutes);
	}
}
