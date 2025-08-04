using Jiro.Core.Services.Conversation;
using Jiro.Core.Services.MessageCache;
using Jiro.Core.Services.Semaphore;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Jiro.Core.Services.Persona;

/// <summary>
/// Service for managing AI persona configuration, including personality messages and conversation summaries.
/// </summary>
public class PersonaService : IPersonaService
{
	private readonly ILogger<PersonaService> _logger;
	private readonly IMessageManager _messageCacheService;
	private readonly IConversationCoreService _coreChatService;
	private readonly IMemoryCache _memoryCache;
	private readonly ISemaphoreManager _chatSemaphoreManager;

	/// <summary>
	/// Initializes a new instance of the <see cref="PersonaService"/> class.
	/// </summary>
	/// <param name="logger">The logger for recording persona operations.</param>
	/// <param name="messageCacheService">The message cache service for persona data.</param>
	/// <param name="chatService">The core conversation service.</param>
	/// <param name="memoryCache">The memory cache for storing computed persona messages.</param>
	/// <param name="chatSemaphoreManager">The semaphore manager for controlling concurrent access.</param>
	public PersonaService(ILogger<PersonaService> logger, IMessageManager messageCacheService, IConversationCoreService chatService, IMemoryCache memoryCache, ISemaphoreManager chatSemaphoreManager)
	{
		_logger = logger;
		_messageCacheService = messageCacheService;
		_coreChatService = chatService;
		_memoryCache = memoryCache;
		_chatSemaphoreManager = chatSemaphoreManager;
	}

	/// <summary>
	/// Retrieves the persona message for the specified instance, using thread-safe access control.
	/// </summary>
	/// <param name="instanceId">The unique identifier of the instance. If empty, returns the default persona.</param>
	/// <returns>A task that represents the asynchronous operation, containing the persona message.</returns>
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

	/// <summary>
	/// Adds a conversation summary to the persona message to maintain context across sessions.
	/// </summary>
	/// <param name="updateMessage">The summary message to append to the persona.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
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

	/// <summary>
	/// Internal method to retrieve and cache the persona message, with fallback to default message.
	/// </summary>
	/// <returns>A task that represents the asynchronous operation, containing the persona message.</returns>
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
