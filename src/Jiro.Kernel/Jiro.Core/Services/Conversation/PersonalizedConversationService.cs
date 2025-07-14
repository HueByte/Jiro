using Jiro.Core.IRepositories;
using Jiro.Core.Models;
using Jiro.Core.Services.CommandContext;
using Jiro.Core.Services.Conversation.Models;
using Jiro.Core.Services.MessageCache;
using Jiro.Core.Services.Persona;

using Microsoft.Extensions.Logging;

using OpenAI.Chat;

namespace Jiro.Core.Services.Conversation;

public class PersonalizedConversationService : IPersonalizedConversationService
{
	private readonly ILogger<PersonalizedConversationService> _logger;
	private readonly IConversationCoreService _chatCoreService;
	private readonly IPersonaService _personaService;
	private readonly IMessageManager _messageCacheService;
	private readonly IHistoryOptimizerService _historyOptimizerService;
	private readonly ICommandContext _commandContext;
	private readonly IChatSessionRepository _chatSessionRepository;
	private readonly IMessageRepository _messageRepository;
	private const float PRICING_OUTPUT = 0.600f;
	private const float PRICING_INPUT = 0.150f;
	private const float PRICING_INPUT_CACHED = 0.075f;
	private const float ONE_MILLION = 1_000_000;

	public PersonalizedConversationService(ILogger<PersonalizedConversationService> logger, IConversationCoreService chatCoreService, IPersonaService personaService, IMessageManager messageCacheService, IHistoryOptimizerService historyOptimizerService, ICommandContext commandContext, IChatSessionRepository chatSessionRepository, IMessageRepository messageRepository)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null.");
		_chatCoreService = chatCoreService ?? throw new ArgumentNullException(nameof(chatCoreService), "Chat core service cannot be null.");
		_personaService = personaService ?? throw new ArgumentNullException(nameof(personaService), "Persona service cannot be null.");
		_messageCacheService = messageCacheService ?? throw new ArgumentNullException(nameof(messageCacheService), "Message cache service cannot be null.");
		_historyOptimizerService = historyOptimizerService ?? throw new ArgumentNullException(nameof(historyOptimizerService), "History optimizer service cannot be null.");
		_commandContext = commandContext ?? throw new ArgumentNullException(nameof(commandContext), "Command context cannot be null.");
		_chatSessionRepository = chatSessionRepository ?? throw new ArgumentNullException(nameof(chatSessionRepository), "Chat session repository cannot be null.");
		_messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository), "Message repository cannot be null.");
	}

	public async Task<string> ChatAsync(string instanceId, string sessionId, string message)
	{
		try
		{
			var persona = await _personaService.GetPersonaAsync(instanceId);
			if (persona == null)
			{
				_logger.LogError("Persona not found for instance {InstanceId}", instanceId);
				throw new Exception("Persona not found.");
			}
			// Get or create session with proper caching and message loading
			var sessionWithMessages = await _messageCacheService.GetOrCreateChatSessionAsync(sessionId, includeMessages: true);
			var (conversationForChat, conversationHistory) = PrepareMessageHistory(sessionWithMessages, message);

			var response = await _chatCoreService.ChatAsync(instanceId, conversationForChat.Select(x => x.Message).ToList(), ChatMessage.CreateDeveloperMessage(persona));
			var assistantMessages = response.Content;
			var assistantResponse = assistantMessages.FirstOrDefault()?.Text;

			if (string.IsNullOrEmpty(assistantResponse))
			{
				_logger.LogError("No assistant response received for instance {InstanceId}", instanceId);
				throw new Exception("No assistant response received.");
			}

			var tokenUsage = response.Usage;
			LogTokenUsage(tokenUsage);

			// Create assistant response metadata
			var jiroMessage = new ChatMessageWithMetadata
			{
				Message = ChatMessage.CreateAssistantMessage(assistantResponse),
				CreatedAt = DateTime.UtcNow,
				Type = MessageType.Text
			};
			// Save messages to database using MessageManager
			await SaveMessagesToSessionAsync(sessionWithMessages.SessionId, conversationHistory.Last(), jiroMessage);

			// Handle history optimization
			if (_historyOptimizerService.ShouldOptimizeMessageHistory(tokenUsage))
			{
				try
				{
					var allMessages = conversationHistory.Concat([jiroMessage]).ToList();
					var optimizationResult = await _historyOptimizerService.OptimizeMessageHistory(tokenUsage.TotalTokenCount, allMessages.Select(x => x.Message).ToList(), persona);
					_messageCacheService.ClearOldMessages(sessionId, optimizationResult.RemovedMessages - 1);
					await _personaService.AddSummaryAsync(optimizationResult.MessagesSummary);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error during history optimization for instance {InstanceId}", instanceId);
				}
			}

			return assistantResponse;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error in ChatAsync for instance {InstanceId}", instanceId);
			throw;
		}
	}

	public async Task<string> ExchangeMessageAsync(string message)
	{
		var persona = await _personaService.GetPersonaAsync();
		return await _chatCoreService.ExchangeMessageAsync(message, persona);
	}

	/// <summary>
	/// Saves user and assistant messages to the session using MessageManager.
	/// </summary>
	private async Task SaveMessagesToSessionAsync(string sessionId, ChatMessageWithMetadata userMessage, ChatMessageWithMetadata assistantMessage)
	{
		try
		{
			var userMessageModel = new Message
			{
				Id = Guid.NewGuid().ToString(),
				InstanceId = _commandContext.InstanceId ?? throw new InvalidOperationException("InstanceId is required"),
				Content = userMessage.Message.Content.FirstOrDefault()?.Text ?? string.Empty,
				IsUser = true,
				CreatedAt = userMessage.CreatedAt,
				SessionId = sessionId,
				Type = userMessage.Type,
			};

			var assistantMessageModel = new Message
			{
				Id = Guid.NewGuid().ToString(),
				InstanceId = _commandContext.InstanceId ?? throw new InvalidOperationException("InstanceId is required"),
				Content = assistantMessage.Message.Content.FirstOrDefault()?.Text ?? string.Empty,
				IsUser = false,
				CreatedAt = assistantMessage.CreatedAt,
				SessionId = sessionId,
				Type = assistantMessage.Type,
			};

			// Let MessageManager handle all persistence logic
			var modelMessages = new List<Message> { userMessageModel, assistantMessageModel };
			await _messageCacheService.AddChatExchangeAsync(sessionId,
				new List<ChatMessageWithMetadata> { userMessage, assistantMessage },
				modelMessages);

			_logger.LogInformation("Saved user and assistant messages to session {SessionId}", sessionId);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error saving messages to session {SessionId}", sessionId);
			throw;
		}
	}

	private void LogTokenUsage(ChatTokenUsage usage)
	{
		_logger.LogInformation("Chat used [Total: {totalTokens}] ~ [Input: {inputTokens}] ~ [CachedInput: {cachedInputTokens}] ~ [Output: {outputTokens}] ~ [JiroCounter: {JiroTokens}] tokens",
			usage.TotalTokenCount, usage.InputTokenCount, usage.InputTokenDetails.CachedTokenCount, usage.OutputTokenCount, usage.TotalTokenCount - (usage.InputTokenDetails.CachedTokenCount / 2));

		_logger.LogInformation("Estimated message price: {messagePrice}$", CalculateMessagePrice(usage));
	}

	private (List<ChatMessageWithMetadata>, List<ChatMessageWithMetadata>) PrepareMessageHistory(Session session, string message)
	{
		try
		{
			// Load existing messages from session (already loaded from cache with messages)
			var existingMessages = session.Messages?.OrderBy(m => m.CreatedAt).ToList() ?? new List<ChatMessageWithMetadata>();

			var conversationHistory = new List<ChatMessageWithMetadata>(existingMessages);

			var conversationForChat = new List<ChatMessageWithMetadata>(conversationHistory);

			// Add new user message
			var userChatMessage = new ChatMessageWithMetadata
			{
				Message = ChatMessage.CreateUserMessage(message),
				CreatedAt = DateTime.UtcNow,
				Type = MessageType.Text,
				IsUser = true
			};

			conversationForChat.Add(userChatMessage);
			conversationHistory.Add(userChatMessage);

			return (conversationForChat, conversationHistory);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error preparing message history for session {SessionId}", session.SessionId);
			throw;
		}
	}

	private float CalculateMessagePrice(ChatTokenUsage tokenUsage)
	{
		var messagePrice =
			(tokenUsage.InputTokenCount - tokenUsage.InputTokenDetails.CachedTokenCount) / ONE_MILLION * PRICING_INPUT
			+ tokenUsage.InputTokenDetails.CachedTokenCount / ONE_MILLION * PRICING_INPUT_CACHED
			+ tokenUsage.OutputTokenCount / ONE_MILLION * PRICING_OUTPUT;

		return messagePrice;
	}
}
