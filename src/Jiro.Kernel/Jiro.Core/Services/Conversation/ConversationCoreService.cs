using Jiro.Core.Services.MessageCache;
using Jiro.Core.Services.Semaphore;

using Microsoft.Extensions.Logging;

using OpenAI.Chat;

namespace Jiro.Core.Services.Conversation;

/// <summary>
/// Core conversation service that handles basic chat interactions with AI models.
/// This service focuses on simple chat functionality without session management.
/// </summary>
public class ConversationCoreService : IConversationCoreService
{
	private readonly ILogger<ConversationCoreService> _logger;
	private readonly IMessageManager _messageCacheService;
	private readonly ChatClient _openAIClient;
	private readonly ISemaphoreManager _chatSemaphoreManager;
	private const float TEMPERATURE = 0.6f;

	/// <summary>
	/// Initializes a new instance of the ConversationCoreService.
	/// </summary>
	public ConversationCoreService(ILogger<ConversationCoreService> logger, IMessageManager messageCacheService, ChatClient openAIClient, ISemaphoreManager chatSemaphoreManager)
	{
		_logger = logger;
		_messageCacheService = messageCacheService;
		_openAIClient = openAIClient;
		_chatSemaphoreManager = chatSemaphoreManager;
	}

	/// <summary>
	/// Conducts a chat conversation using the specified message history and optional persona message.
	/// </summary>
	/// <param name="instanceId">The unique identifier for the conversation instance.</param>
	/// <param name="messageHistory">The list of previous chat messages that form the conversation context.</param>
	/// <param name="personaMessage">An optional persona message to influence the AI's behavior and responses.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the chat completion response.</returns>
	public async Task<ChatCompletion> ChatAsync(string instanceId, List<ChatMessage> messageHistory, ChatMessage? personaMessage = null)
	{
		// Use a semaphore to prevent concurrent updates for the same channel.
		SemaphoreSlim instanceSemaphore = _chatSemaphoreManager.GetOrCreateInstanceSemaphore(instanceId);
		await instanceSemaphore.WaitAsync();
		try
		{
			_logger.LogInformation("Starting chat for instance {InstanceId}", instanceId);

			personaMessage ??= await GetCorePersonaAsync();
			messageHistory.Insert(0, personaMessage);

			// Set up chat options.
			var options = new ChatCompletionOptions
			{
				MaxOutputTokenCount = 1200,
				Temperature = TEMPERATURE,
			};

			// Call the chat API.
			var result = await _openAIClient.CompleteChatAsync(messageHistory, options);
			if (result == null)
			{
				_logger.LogWarning("Chat API returned null for instance {InstanceId}", instanceId);
				throw new InvalidOperationException("Chat API returned null.");
			}

			_logger.LogInformation("Chat completed for instance {InstanceId}", instanceId);
			return result;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error during chat for instance {InstanceId}", instanceId);
			throw;
		}
		finally
		{
			instanceSemaphore.Release();
		}
	}

	/// <summary>
	/// Exchanges a single message with the AI system and receives a response.
	/// </summary>
	/// <param name="message">The message to send to the AI system.</param>
	/// <param name="developerPersonaChatMessage">An optional developer persona message to guide the AI's responses.</param>
	/// <param name="tokenLimit">The maximum number of tokens to use in the response. Default is 1200.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the AI's response as a string.</returns>
	public async Task<string> ExchangeMessageAsync(string message, ChatMessage? developerPersonaChatMessage = null, int tokenLimit = 1200)
	{
		try
		{
			developerPersonaChatMessage ??= await GetCorePersonaAsync();

			UserChatMessage userMessage = ChatMessage.CreateUserMessage(message);

			var messages = new List<ChatMessage> { developerPersonaChatMessage, userMessage };
			ChatCompletionOptions options = new()
			{
				MaxOutputTokenCount = tokenLimit,
				Temperature = 0.6f,
			};

			var response = await _openAIClient.CompleteChatAsync(messages, options);

			if (response?.Value?.Content?.FirstOrDefault() == null)
			{
				_logger.LogWarning("No content returned from chat API for message: {Message}", message);
				throw new InvalidOperationException("No content returned from chat API.");
			}

			return response.Value.Content.First().Text;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error exchanging message: {Message}", message);
			throw;
		}
	}

	private async Task<ChatMessage> GetCorePersonaAsync()
	{
		var personaMessage = await _messageCacheService.GetPersonaCoreMessageAsync();
		return ChatMessage.CreateSystemMessage(personaMessage);
	}
}
