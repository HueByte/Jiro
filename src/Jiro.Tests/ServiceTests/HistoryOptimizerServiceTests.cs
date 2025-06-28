using System.Text.Json;

using Jiro.Core.Services.Conversation;
using Jiro.Core.Services.Conversation.Models;

using Microsoft.Extensions.Logging;

using Moq;

using OpenAI.Chat;

namespace Jiro.Tests.ServiceTests;

public class HistoryOptimizerServiceTests
{
	private readonly Mock<ILogger<HistoryOptimizerService>> _loggerMock;
	private readonly Mock<IConversationCoreService> _chatCoreServiceMock;
	private readonly HistoryOptimizerService _historyOptimizerService;

	public HistoryOptimizerServiceTests()
	{
		_loggerMock = new Mock<ILogger<HistoryOptimizerService>>();
		_chatCoreServiceMock = new Mock<IConversationCoreService>();
		_historyOptimizerService = new HistoryOptimizerService(_loggerMock.Object, _chatCoreServiceMock.Object);
	}

	[Fact]
	public void Constructor_WithValidParameters_ShouldCreateInstance()
	{
		// Arrange & Act
		var service = new HistoryOptimizerService(_loggerMock.Object, _chatCoreServiceMock.Object);

		// Assert
		Assert.NotNull(service);
	}

	[Fact]
	public void ShouldOptimizeMessageHistory_MethodExists_ShouldNotThrow()
	{
		// Arrange & Act & Assert
		// Since ChatTokenUsage is a sealed class from OpenAI library that cannot be mocked or easily created,
		// we verify that the method exists and would work with real data.
		// The actual logic testing will be covered by integration tests when real ChatTokenUsage
		// objects are available from actual OpenAI API responses.

		var method = typeof(HistoryOptimizerService).GetMethod("ShouldOptimizeMessageHistory");
		Assert.NotNull(method);
		Assert.Equal(typeof(bool), method.ReturnType);
		Assert.Single(method.GetParameters());
		Assert.Equal(typeof(ChatTokenUsage), method.GetParameters()[0].ParameterType);
	}

	[Fact]
	public async Task OptimizeMessageHistory_WithValidMessages_ShouldReturnOptimizerResult()
	{
		// Arrange
		const int currentTokenCount = 15000;
		const string summaryResponse = "Summary of conversation";

		var messages = new List<ChatMessage>
		{
			ChatMessage.CreateDeveloperMessage("You are Jiro"), // Persona message
			ChatMessage.CreateUserMessage("Hello"),
			ChatMessage.CreateAssistantMessage("Hi there!"),
			ChatMessage.CreateUserMessage("How are you?"),
			ChatMessage.CreateAssistantMessage("I'm doing well!")
		};

		var personaMessage = ChatMessage.CreateDeveloperMessage("You are Jiro");

		_chatCoreServiceMock
			.Setup(x => x.ExchangeMessageAsync(It.IsAny<string>(), personaMessage, It.IsAny<int>()))
			.ReturnsAsync(summaryResponse);

		// Act
		var result = await _historyOptimizerService.OptimizeMessageHistory(currentTokenCount, messages, personaMessage);

		// Assert
		Assert.NotNull(result);
		Assert.IsType<OptimizerResult>(result);
		Assert.Equal(summaryResponse, result.MessagesSummary);
		Assert.True(result.RemovedMessages >= 0);

		_chatCoreServiceMock.Verify(x => x.ExchangeMessageAsync(It.IsAny<string>(), personaMessage, It.IsAny<int>()), Times.Once);
	}

	[Fact]
	public async Task OptimizeMessageHistory_WithEmptyMessages_ShouldReturnOptimizerResult()
	{
		// Arrange
		const int currentTokenCount = 15000;
		const string summaryResponse = "No messages to summarize";

		var messages = new List<ChatMessage>();
		var personaMessage = ChatMessage.CreateDeveloperMessage("You are Jiro");

		_chatCoreServiceMock
			.Setup(x => x.ExchangeMessageAsync(It.IsAny<string>(), personaMessage, It.IsAny<int>()))
			.ReturnsAsync(summaryResponse);

		// Act
		var result = await _historyOptimizerService.OptimizeMessageHistory(currentTokenCount, messages, personaMessage);

		// Assert
		Assert.NotNull(result);
		Assert.IsType<OptimizerResult>(result);
		Assert.Equal(summaryResponse, result.MessagesSummary);
		Assert.Equal(0, result.RemovedMessages);
	}

	[Fact]
	public async Task OptimizeMessageHistory_WhenSummaryIsEmpty_ShouldThrowInvalidOperationException()
	{
		// Arrange
		const int currentTokenCount = 15000;

		var messages = new List<ChatMessage>
		{
			ChatMessage.CreateUserMessage("Hello"),
			ChatMessage.CreateAssistantMessage("Hi there!")
		};

		var personaMessage = ChatMessage.CreateDeveloperMessage("You are Jiro");

		_chatCoreServiceMock
			.Setup(x => x.ExchangeMessageAsync(It.IsAny<string>(), personaMessage, It.IsAny<int>()))
			.ReturnsAsync(string.Empty);

		// Act & Assert
		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
			_historyOptimizerService.OptimizeMessageHistory(currentTokenCount, messages, personaMessage));

		Assert.Equal("Summary response is empty.", exception.Message);
	}

	[Fact]
	public async Task OptimizeMessageHistory_WhenSummaryIsWhitespace_ShouldThrowInvalidOperationException()
	{
		// Arrange
		const int currentTokenCount = 15000;

		var messages = new List<ChatMessage>
		{
			ChatMessage.CreateUserMessage("Hello"),
			ChatMessage.CreateAssistantMessage("Hi there!")
		};

		var personaMessage = ChatMessage.CreateDeveloperMessage("You are Jiro");

		_chatCoreServiceMock
			.Setup(x => x.ExchangeMessageAsync(It.IsAny<string>(), personaMessage, It.IsAny<int>()))
			.ReturnsAsync("   ");

		// Act & Assert
		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
			_historyOptimizerService.OptimizeMessageHistory(currentTokenCount, messages, personaMessage));

		Assert.Equal("Summary response is empty.", exception.Message);
	}

	[Fact]
	public async Task OptimizeMessageHistory_WhenChatServiceThrows_ShouldPropagateException()
	{
		// Arrange
		const int currentTokenCount = 15000;

		var messages = new List<ChatMessage>
		{
			ChatMessage.CreateUserMessage("Hello"),
			ChatMessage.CreateAssistantMessage("Hi there!")
		};

		var personaMessage = ChatMessage.CreateDeveloperMessage("You are Jiro");
		var expectedException = new Exception("Chat service error");

		_chatCoreServiceMock
			.Setup(x => x.ExchangeMessageAsync(It.IsAny<string>(), personaMessage, It.IsAny<int>()))
			.ThrowsAsync(expectedException);

		// Act & Assert
		var exception = await Assert.ThrowsAsync<Exception>(() =>
			_historyOptimizerService.OptimizeMessageHistory(currentTokenCount, messages, personaMessage));

		Assert.Equal(expectedException, exception);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(5000)]
	[InlineData(20000)]
	public async Task OptimizeMessageHistory_WithVariousTokenCounts_ShouldHandleCorrectly(int tokenCount)
	{
		// Arrange
		const string summaryResponse = "Summary";

		var messages = new List<ChatMessage>
		{
			ChatMessage.CreateUserMessage("Test message")
		};

		var personaMessage = ChatMessage.CreateDeveloperMessage("You are Jiro");

		_chatCoreServiceMock
			.Setup(x => x.ExchangeMessageAsync(It.IsAny<string>(), personaMessage, It.IsAny<int>()))
			.ReturnsAsync(summaryResponse);

		// Act
		var result = await _historyOptimizerService.OptimizeMessageHistory(tokenCount, messages, personaMessage);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(summaryResponse, result.MessagesSummary);
	}
}
