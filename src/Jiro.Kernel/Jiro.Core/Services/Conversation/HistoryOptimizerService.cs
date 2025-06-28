using System.Text;

using Jiro.Core.Services.Conversation.Models;
using Jiro.Core.Utils;

using Microsoft.Extensions.Logging;

using OpenAI.Chat;

namespace Jiro.Core.Services.Conversation;

public class HistoryOptimizerService : IHistoryOptimizerService
{
	private readonly ILogger<HistoryOptimizerService> _logger;
	private readonly IConversationCoreService _chatCoreService;
	private readonly int _maxTokens = 10000;

	public HistoryOptimizerService(ILogger<HistoryOptimizerService> logger, IConversationCoreService chatCoreService)
	{
		_logger = logger;
		_chatCoreService = chatCoreService;
	}

	public bool ShouldOptimizeMessageHistory(ChatTokenUsage tokenUsage)
	{
		return tokenUsage.TotalTokenCount - (tokenUsage.InputTokenDetails.CachedTokenCount / 2) > _maxTokens;
	}

	public async Task<OptimizerResult> OptimizeMessageHistory(int currentTokenCount, List<ChatMessage> messages, ChatMessage? personaMessage = null)
	{
		var targetTokenCount = _maxTokens / 2;
		var messagesToRemoveCount = 0;

		_logger.LogInformation("Starting message history optimization | Current token count: {currentTokenCount} | Target token count: {targetTokenCount}", currentTokenCount, targetTokenCount);
		if (messages.Count % 2 == 0)
		{
			_logger.LogWarning("Even number of messages in the conversation history. Probably missing a persona message.");
		}

		try
		{
			foreach (ChatMessage message in messages)
			{
				var text = message.Content.First().Text;
				var tokenCount = await Tokenizer.CountTokensAsync(text);

				// messagesToRemoveCount should be odd, since we include the persona message and user - assistant pair messages.
				if (currentTokenCount < targetTokenCount && messagesToRemoveCount % 2 != 0)
				{
					break;
				}

				currentTokenCount -= tokenCount;
				messagesToRemoveCount++;
			}

			// Skip the persona message as its developer message.
			var messagesToRemove = messages.Skip(1).Take(messagesToRemoveCount).ToList();
			var summary = await SummarizeMessagesAsync(messagesToRemove, personaMessage);

			_logger.LogInformation("Message history optimization completed | Messages removed: {messagesToRemoveCount} | New token count: {currentTokenCount}\nPerformed summary {summary}", messagesToRemoveCount, currentTokenCount, summary);

			return new OptimizerResult()
			{
				RemovedMessages = messagesToRemoveCount,
				MessagesSummary = summary,
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error during message history optimization.");
			throw;
		}
	}

	private async Task<string> SummarizeMessagesAsync(List<ChatMessage> messages, ChatMessage? personaMessage = null)
	{
		StringBuilder sb = new();

		sb.AppendLine("Jiro, you’re nearing your memory limit. Summarize the following messages from your perspective—focus on key points, user requests, and relevant context, so you can carry on the conversation seamlessly. Keep the recap concise and in your own notes-style format.");
		foreach (ChatMessage message in messages)
		{
			sb.AppendLine(message.Content.First().Text);
		}

		try
		{
			var response = await _chatCoreService.ExchangeMessageAsync(sb.ToString(), personaMessage, _maxTokens / 4);

			if (string.IsNullOrWhiteSpace(response))
			{
				_logger.LogWarning("Summary response is empty.");
				throw new InvalidOperationException("Summary response is empty.");
			}

			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error during summarization of messages.");
			throw;
		}
	}
}
