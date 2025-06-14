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
	private readonly IMessageCacheService _messageCacheService;
	private readonly IHistoryOptimizerService _historyOptimizerService;
	private readonly ICommandContext _commandContext;
	private const float PRICING_OUTPUT = 0.600f;
	private const float PRICING_INPUT = 0.150f;
	private const float PRICING_INPUT_CACHED = 0.075f;
	private const float ONE_MILLION = 1_000_000;

	public PersonalizedConversationService (ILogger<PersonalizedConversationService> logger, IConversationCoreService chatCoreService, IPersonaService personaService, IMessageCacheService messageCacheService, IHistoryOptimizerService historyOptimizerService, ICommandContext commandContext)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null.");
		_chatCoreService = chatCoreService ?? throw new ArgumentNullException(nameof(chatCoreService), "Chat core service cannot be null.");
		_personaService = personaService ?? throw new ArgumentNullException(nameof(personaService), "Persona service cannot be null.");
		_messageCacheService = messageCacheService ?? throw new ArgumentNullException(nameof(messageCacheService), "Message cache service cannot be null.");
		_historyOptimizerService = historyOptimizerService ?? throw new ArgumentNullException(nameof(historyOptimizerService), "History optimizer service cannot be null.");
		_commandContext = commandContext ?? throw new ArgumentNullException(nameof(commandContext), "Command context cannot be null.");
	}

	public async Task<string> ChatAsync (string instanceId, string sessionId, string message)
	{
		try
		{
			var persona = await _personaService.GetPersonaAsync(instanceId);
			if (persona == null)
			{
				_logger.LogError("Persona not found for instance {InstanceId}", instanceId);
				throw new Exception("Persona not found.");
			}

			var (conversationForChat, conversationHistory, session) = await PrepareMessageHistory(sessionId, message);

			var response = await _chatCoreService.ChatAsync(instanceId, conversationForChat, ChatMessage.CreateDeveloperMessage(persona));
			var assistantMessages = response.Content;
			var assistantResponse = assistantMessages.FirstOrDefault()?.Text;

			if (string.IsNullOrEmpty(assistantResponse))
			{
				_logger.LogError("No assistant response received for instance {InstanceId}", instanceId);
				throw new Exception("No assistant response received.");
			}

			var tokenUsage = response.Usage;
			LogTokenUsage(tokenUsage);

			var userMessage = conversationForChat.Last();
			var jiroMessage = ChatMessage.CreateAssistantMessage(assistantResponse);

			conversationForChat.Add(jiroMessage);
			conversationHistory.Add(jiroMessage);

			var models = CreateMessageModels(session.SessionId, userMessage, jiroMessage);

			await _messageCacheService.AddChatExchangeAsync(sessionId, conversationHistory, models);
			if (_historyOptimizerService.ShouldOptimizeMessageHistory(tokenUsage))
			{
				try
				{
					var optimizationResult = await _historyOptimizerService.OptimizeMessageHistory(tokenUsage.TotalTokenCount, conversationForChat, persona);
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

	public async Task<string> ExchangeMessageAsync (string message)
	{
		var persona = await _personaService.GetPersonaAsync();
		return await _chatCoreService.ExchangeMessageAsync(message, persona);
	}

	private void LogTokenUsage (ChatTokenUsage usage)
	{
		_logger.LogInformation("Chat used [Total: {totalTokens}] ~ [Input: {inputTokens}] ~ [CachedInput: {cachedInputTokens}] ~ [Output: {outputTokens}] ~ [JiroCounter: {JiroTokens}] tokens",
			usage.TotalTokenCount, usage.InputTokenCount, usage.InputTokenDetails.CachedTokenCount, usage.OutputTokenCount, usage.TotalTokenCount - (usage.InputTokenDetails.CachedTokenCount / 2));

		_logger.LogInformation("Estimated message price: {messagePrice}$", CalculateMessagePrice(usage));
	}

	private async Task<(List<ChatMessage>, List<ChatMessage>, Session session)> PrepareMessageHistory (string sessionId, string message)
	{
		Session session = await _messageCacheService.GetOrCreateChatSessionAsync(sessionId);
		if (session is null)
		{
			_logger.LogError("Chat session with ID {SessionId} could not be created or retrieved.", sessionId);
			throw new InvalidOperationException($"Chat session with ID {sessionId} could not be created or retrieved.");
		}

		var conversationHistory = new List<ChatMessage>(session.Messages);
		var conversationForChat = new List<ChatMessage>();
		conversationForChat.AddRange(conversationHistory);

		var userChatMessage = ChatMessage.CreateUserMessage(message);
		conversationForChat.Add(userChatMessage);
		conversationHistory.Add(userChatMessage);

		return (conversationForChat, conversationHistory, session)!;

		// var cachedMessages = await _messageCacheService.GetOrCreateChatSessionAsync(instanceId, sessionId)
		// 					 ?? [];

		// var conversationHistory = new List<ChatMessage>(cachedMessages);

		// var conversationForChat = new List<ChatMessage>();
		// conversationForChat.AddRange(conversationHistory);

		// // Create the user message.
		// var userChatMessage = ChatMessage.CreateUserMessage(message);
		// conversationForChat.Add(userChatMessage);

		// // Also add the user message to the conversation history for persistence.
		// conversationHistory.Add(userChatMessage);

		// return (conversationForChat, conversationHistory);
	}

	private float CalculateMessagePrice (ChatTokenUsage tokenUsage)
	{
		var messagePrice =
			(tokenUsage.InputTokenCount - tokenUsage.InputTokenDetails.CachedTokenCount) / ONE_MILLION * PRICING_INPUT
			+ tokenUsage.InputTokenDetails.CachedTokenCount / ONE_MILLION * PRICING_INPUT_CACHED
			+ tokenUsage.OutputTokenCount / ONE_MILLION * PRICING_OUTPUT;

		return messagePrice;
	}

	private List<Core.Models.Message> CreateMessageModels (string sessionId, ChatMessage userMessage, ChatMessage JiroMessage)
	{
		var modelMessages = new List<Core.Models.Message>
		{
			// TODO adjust Message Type based on the actual message type.
			new()
			{
				Id = Guid.NewGuid().ToString(),
				InstanceId = _commandContext.InstanceId,
				Content = userMessage.Content.FirstOrDefault()?.Text ?? string.Empty,
				IsUser = true,
				CreatedAt = DateTime.UtcNow,
				SessionId = sessionId,
				Type = MessageType.Text,
			},
			new()
			{
				Id = Guid.NewGuid().ToString(),
				InstanceId = _commandContext.InstanceId,
				Content = JiroMessage.Content.FirstOrDefault()?.Text ?? string.Empty,
				IsUser = false,
				CreatedAt = DateTime.UtcNow,
				SessionId = sessionId,
				Type = MessageType.Text,
			}
		};

		return modelMessages;
	}
}
