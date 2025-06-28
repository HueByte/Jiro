using Jiro.Core.Services.Conversation;
using Jiro.Core.Services.MessageCache;
using Jiro.Core.Services.Semaphore;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Jiro.Core.Services.Persona;

public class PersonaService : IPersonaService
{
	private readonly ILogger<PersonaService> _logger;
	private readonly IMessageManager _messageCacheService;
	private readonly IConversationCoreService _coreChatService;
	private readonly IMemoryCache _memoryCache;
	private readonly ISemaphoreManager _chatSemaphoreManager;

	public PersonaService(ILogger<PersonaService> logger, IMessageManager messageCacheService, IConversationCoreService chatService, IMemoryCache memoryCache, ISemaphoreManager chatSemaphoreManager)
	{
		_logger = logger;
		_messageCacheService = messageCacheService;
		_coreChatService = chatService;
		_memoryCache = memoryCache;
		_chatSemaphoreManager = chatSemaphoreManager;
	}

	public async Task<string> GetPersonaAsync(string instanceId = "")
	{
		if (string.IsNullOrEmpty(instanceId))
		{
			_logger.LogWarning("Instance ID is empty. Returning default persona message.");
			return await GetPersonaInternalAsync();
		}

		SemaphoreSlim channelSemaphore = _chatSemaphoreManager.GetOrCreateInstanceSemaphore(instanceId);
		await channelSemaphore.WaitAsync();

		var personaMessage = string.Empty;
		try
		{
			personaMessage = await GetPersonaInternalAsync();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving persona for instance {InstanceId}", instanceId);
			throw;
		}
		finally
		{
			channelSemaphore.Release();
		}

		return personaMessage;
	}

	public async Task AddSummaryAsync(string updateMessage)
	{
		try
		{
			var personaMessage = _memoryCache.Get<string>(Constants.CacheKeys.ComputedPersonaMessageKey);
			if (string.IsNullOrEmpty(personaMessage))
			{
				personaMessage = await GetPersonaInternalAsync();
				_logger.LogInformation("Persona message cache miss. Loaded from source.");
			}

			personaMessage += $"\nThis is your summary of recent conversations: {updateMessage}";

			_memoryCache.Set(Constants.CacheKeys.ComputedPersonaMessageKey, personaMessage, TimeSpan.FromDays(1));
			_logger.LogInformation("Persona summary updated.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating persona summary.");
			throw;
		}
	}

	private async Task<string> GetPersonaInternalAsync()
	{
		try
		{
			if (_memoryCache.TryGetValue(Constants.CacheKeys.ComputedPersonaMessageKey, out string? personaMessage))
			{
				if (!string.IsNullOrEmpty(personaMessage))
				{
					return personaMessage;
				}
			}

			personaMessage = await _messageCacheService.GetPersonaCoreMessageAsync();

			if (string.IsNullOrEmpty(personaMessage))
				personaMessage = $"I'm {Constants.AgentMetadata.Name}. Your AI assistant.";

			_memoryCache.Set(Constants.CacheKeys.ComputedPersonaMessageKey, personaMessage, TimeSpan.FromDays(1));
			_logger.LogInformation("Computed persona message: {personaMessage}", personaMessage);

			return personaMessage;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving internal persona message.");
			throw;
		}
	}
}
