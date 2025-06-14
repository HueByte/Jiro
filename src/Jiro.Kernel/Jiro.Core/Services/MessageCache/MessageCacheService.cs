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

public class MessageCacheService : IMessageCacheService
{
	private readonly ILogger<MessageCacheService> _logger;
	private readonly IMemoryCache _memoryCache;
	private readonly IMessageRepository _messageRepository;
	private readonly IChatSessionRepository _chatSessionRepository;
	private readonly ICommandContext _commandContext;
	private readonly int _messageFetchCount = 40;
	private const int MEMORY_CACHE_EXPIRATION = 5;

	public MessageCacheService (ILogger<MessageCacheService> logger, IMemoryCache memoryCache, IMessageRepository messageRepository, IChatSessionRepository chatSessionRepository, IConfiguration configuration, ICommandContext commandContext)
	{
		_logger = logger;
		_memoryCache = memoryCache;
		_messageRepository = messageRepository;
		_chatSessionRepository = chatSessionRepository;
		_messageFetchCount = configuration.GetValue<int>(Constants.Environment.MessageFetchCount);
		_commandContext = commandContext ?? throw new ArgumentNullException(nameof(commandContext), "Command context cannot be null.");
	}

	public void ClearMessageCache ()
	{
		// Log cache clear action
		_logger?.LogInformation("Clearing persona and core persona message cache.");
		_memoryCache.Remove(Constants.CacheKeys.ComputedPersonaMessageKey);
		_memoryCache.Remove(Constants.CacheKeys.CorePersonaMessageKey);
	}

	public async Task<List<ChatSession>> GetChatSessionsAsync (string instanceId)
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

	public async Task<string?> GetPersonaCoreMessageAsync ()
	{
		return await GetMessageAsync(Constants.CacheKeys.CorePersonaMessageKey);
	}

	public int GetChatMessageCount (string instanceId)
	{
		if (_memoryCache.TryGetValue(instanceId, out List<ChatMessage>? channelMessages))
		{
			return channelMessages?.Count ?? 0;
		}

		return 0;
	}

	public async Task<Session> GetOrCreateChatSessionAsync (string sessionId)
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
						Id = _commandContext.SessionId,
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
					Messages = session.Messages.Select(x =>
							x.IsUser
								? (ChatMessage)ChatMessage.CreateUserMessage(x.Content)
								: ChatMessage.CreateAssistantMessage(x.Content)
						).ToList()
				};

				// List<Models.Message> messages = await _messageRepository.AsQueryable()
				// 	.Where(x => x.InstanceId == instanceId && x.SessionId == sessionId)
				// 	.OrderBy(x => x.CreatedAt)
				// 	.Take(_messageFetchCount)
				// 	.ToListAsync();

				// _logger.LogInformation("Populating cache for instance {InstanceId} with {Count} messages.", instanceId, messages.Count);

				// return messages.Select(x =>
				// 		x.IsUser
				// 			? (ChatMessage)ChatMessage.CreateUserMessage(x.Content)
				// 			: ChatMessage.CreateAssistantMessage(x.Content)
				// 	).ToList();
			}) ?? throw new InvalidOperationException($"Chat session with ID {sessionId} could not be created or retrieved.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching or caching chat messages for instance {InstanceId}", _commandContext.InstanceId);
			throw;
		}
	}

	public async Task AddChatExchangeAsync (string sessionId, List<ChatMessage> messages, List<Message> modelMessages)
	{
		try
		{
			var instanceId = _commandContext.InstanceId ?? throw new InvalidOperationException("Command context or current instance is not set.");
			if (_memoryCache.TryGetValue(sessionId, out Session? session))
			{
				if (session is null)
				{
					_logger.LogWarning("Chat session with ID {SessionId} not found in cache. Creating a new session.", sessionId);
					session = await GetOrCreateChatSessionAsync(sessionId);
				}

				var sessionMessages = session.Messages ?? [];
				if (sessionMessages.Count > 0)
				{
					sessionMessages.Clear();
				}

				sessionMessages.AddRange(messages);
				_memoryCache.Set(sessionId, session, TimeSpan.FromDays(MEMORY_CACHE_EXPIRATION));
			}
			else
			{
				session = await GetOrCreateChatSessionAsync(sessionId);
				_memoryCache.Set(sessionId, session, TimeSpan.FromDays(MEMORY_CACHE_EXPIRATION));
			}

			await _messageRepository.AddRangeAsync(modelMessages);
			await _messageRepository.SaveChangesAsync();
		}
		catch (Exception ex)
		{
			_logger?.LogError(ex, "Error adding chat exchange for instance {InstanceId}", sessionId);
			throw;
		}
	}

	public void ClearOldMessages (string sessionId, int range)
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

	public void ModifyMessage (string key, string message, int minutes = 30)
	{
		_memoryCache.Set(key, message, TimeSpan.FromMinutes(minutes));
	}

	private async Task<string?> GetMessageAsync (string key)
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

	private async Task<List<ChatSession>> FetchChatSessionsAsync (string instanceId)
	{
		try
		{
			var sessions = await _chatSessionRepository.AsQueryable()
				.Include(x => x.Messages.OrderBy(m => m.CreatedAt))
				.ToListAsync();

			if (sessions == null || !sessions.Any())
			{
				_logger.LogInformation("No chat sessions found for instance {InstanceId}", instanceId);
				return new List<ChatSession>();
			}

			return sessions;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching chat sessions for instance {InstanceId}", instanceId);
			throw;
		}
	}
}
