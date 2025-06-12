using Jiro.Core.IRepositories;

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
	private readonly int _messageFetchCount = 40;
	private const int MEMORY_CACHE_EXPIRATION = 5;

	public MessageCacheService (ILogger<MessageCacheService> logger, IMemoryCache memoryCache, IMessageRepository messageRepository, IConfiguration configuration)
	{
		_logger = logger;
		_memoryCache = memoryCache;
		_messageRepository = messageRepository;
		_messageFetchCount = configuration.GetValue<int>(Constants.Environment.MessageFetchCount);
	}

	public void ClearMessageCache ()
	{
		// Log cache clear action
		_logger?.LogInformation("Clearing persona and core persona message cache.");
		_memoryCache.Remove(Constants.CacheKeys.ComputedPersonaMessageKey);
		_memoryCache.Remove(Constants.CacheKeys.CorePersonaMessageKey);
	}

	public async Task<string?> GetPersonaCoreMessageAsync ()
	{
		return await GetMessageAsync(Constants.CacheKeys.CorePersonaMessageKey);
	}

	public int GetChatMessageCount (ulong instanceId)
	{
		if (_memoryCache.TryGetValue(instanceId, out List<ChatMessage>? channelMessages))
		{
			return channelMessages?.Count ?? 0;
		}

		return 0;
	}

	public async Task<List<ChatMessage>?> GetOrCreateChatMessagesAsync (ulong instanceId)
	{
		try
		{
			return await _memoryCache.GetOrCreateAsync(instanceId, async entry =>
			{
				entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(MEMORY_CACHE_EXPIRATION);
				List<Models.Message> messages = await _messageRepository.AsQueryable()
					.Where(x => x.InstanceId == instanceId)
					.OrderBy(x => x.CreatedAt)
					.Take(_messageFetchCount)
					.ToListAsync();

				_logger.LogInformation("Populating cache for instance {InstanceId} with {Count} messages.", instanceId, messages.Count);

				return messages.Select(x =>
						x.IsUser
							? (ChatMessage)ChatMessage.CreateUserMessage(x.Content)
							: ChatMessage.CreateAssistantMessage(x.Content)
					).ToList();
			});
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching or caching chat messages for instance {InstanceId}", instanceId);
			throw;
		}
	}

	public async Task AddChatExchangeAsync (ulong instanceId, List<ChatMessage> messages, List<Core.Models.Message> modelMessages)
	{
		try
		{
			if (_memoryCache.TryGetValue(instanceId, out List<ChatMessage>? chatMessages))
			{
				if (chatMessages?.Count > 0)
				{
					chatMessages.Clear();
				}

				chatMessages = chatMessages ?? messages;
				chatMessages.AddRange(messages);

				_memoryCache.Set(instanceId, chatMessages, TimeSpan.FromDays(MEMORY_CACHE_EXPIRATION));
			}
			else
			{
				_memoryCache.Set(instanceId, messages, TimeSpan.FromDays(MEMORY_CACHE_EXPIRATION));
			}

			await _messageRepository.AddRangeAsync(modelMessages);
			await _messageRepository.SaveChangesAsync();
		}
		catch (Exception ex)
		{
			_logger?.LogError(ex, "Error adding chat exchange for instance {InstanceId}", instanceId);
			throw;
		}
	}

	public void ClearOldMessages (ulong instanceId, int range)
	{
		if (!_memoryCache.TryGetValue(instanceId, out List<ChatMessage>? serverMessages))
		{
			return;
		}

		if (serverMessages is not null && serverMessages.Count > range)
		{
			serverMessages.RemoveRange(0, serverMessages.Count - range);
			_memoryCache.Set(instanceId, serverMessages, TimeSpan.FromDays(MEMORY_CACHE_EXPIRATION));
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
}
