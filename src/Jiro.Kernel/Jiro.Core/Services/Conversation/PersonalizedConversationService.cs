using Jiro.Core.Models;
using Jiro.Core.Services.MessageCache;
using Jiro.Core.Services.Persona;

using Microsoft.Extensions.Logging;

using OpenAI.Chat;

namespace Jiro.Core.Services.Conversation;

public class PersonalizedConversationService : IPersonalizedConversationService
{
	private readonly ILogger<PersonalizedConversationService> _logger;
	private readonly IChatCoreService _chatCoreService;
	private readonly IPersonaService _personaService;
	private readonly IMessageCacheService _messageCacheService;
	private readonly IHistoryOptimizerService _historyOptimizerService;
	private const float PRICING_OUTPUT = 0.600f;
	private const float PRICING_INPUT = 0.150f;
	private const float PRICING_INPUT_CACHED = 0.075f;
	private const float ONE_MILLION = 1_000_000;

	public PersonalizedConversationService (ILogger<PersonalizedConversationService> logger, IChatCoreService chatCoreService, IPersonaService personaService, IMessageCacheService messageCacheService, IHistoryOptimizerService historyOptimizerService)
	{
		_logger = logger;
		_chatCoreService = chatCoreService;
		_personaService = personaService;
		_messageCacheService = messageCacheService;
		_historyOptimizerService = historyOptimizerService;
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

			var (conversationForChat, conversationHistory) = await PrepareMessageHistory(instanceId, message);

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

			var models = CreateMessageModels(instanceId, sessionId, userMessage, jiroMessage);

			await _messageCacheService.AddChatExchangeAsync(instanceId, conversationHistory, models);

			if (_historyOptimizerService.ShouldOptimizeMessageHistory(tokenUsage))
			{
				try
				{
					var optimizationResult = await _historyOptimizerService.OptimizeMessageHistory(tokenUsage.TotalTokenCount, conversationForChat, persona);
					_messageCacheService.ClearOldMessages(instanceId, optimizationResult.RemovedMessages - 1);
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

	private async Task<(List<ChatMessage>, List<ChatMessage>)> PrepareMessageHistory (string instanceId, string message)
	{
		var cachedMessages = await _messageCacheService.GetOrCreateChatMessagesAsync(instanceId)
							 ?? [];

		var conversationHistory = new List<ChatMessage>(cachedMessages);

		var conversationForChat = new List<ChatMessage>();
		conversationForChat.AddRange(conversationHistory);

		// Create the user message.
		var userChatMessage = ChatMessage.CreateUserMessage(message);
		conversationForChat.Add(userChatMessage);

		// Also add the user message to the conversation history for persistence.
		conversationHistory.Add(userChatMessage);

		return (conversationForChat, conversationHistory);
	}

	private float CalculateMessagePrice (ChatTokenUsage tokenUsage)
	{
		var messagePrice =
			(tokenUsage.InputTokenCount - tokenUsage.InputTokenDetails.CachedTokenCount) / ONE_MILLION * PRICING_INPUT
			+ tokenUsage.InputTokenDetails.CachedTokenCount / ONE_MILLION * PRICING_INPUT_CACHED
			+ tokenUsage.OutputTokenCount / ONE_MILLION * PRICING_OUTPUT;

		return messagePrice;
	}

	private List<Core.Models.Message> CreateMessageModels (string instanceId, string sessionId, ChatMessage userMessage, ChatMessage JiroMessage)
	{
		var modelMessages = new List<Core.Models.Message>
		{
			// TODO adjust Message Type based on the actual message type.
			new()
			{
				Id = Guid.NewGuid().ToString(),
				InstanceId = instanceId,
				Content = userMessage.Content.FirstOrDefault()?.Text ?? string.Empty,
				IsUser = true,
				CreatedAt = DateTime.UtcNow,
				SessionId = sessionId,
				Type = MessageType.Text,
			},
			new()
			{
				Id = Guid.NewGuid().ToString(),
				InstanceId = instanceId,
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
