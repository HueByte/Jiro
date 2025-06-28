using Jiro.Core;
using Jiro.Core.IRepositories;
using Jiro.Core.Models;
using Jiro.Core.Services.CommandContext;
using Jiro.Core.Services.Conversation.Models;
using Jiro.Core.Services.MessageCache;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Moq;

using OpenAI.Chat;

namespace Jiro.Tests.ServiceTests;

public class MessageManagerTests
{
	private readonly Mock<ILogger<MessageManager>> _loggerMock;
	private readonly Mock<IMemoryCache> _memoryCacheMock;
	private readonly Mock<IMessageRepository> _messageRepositoryMock;
	private readonly Mock<IChatSessionRepository> _chatSessionRepositoryMock;
	private readonly Mock<IConfiguration> _configurationMock;
	private readonly Mock<ICommandContext> _commandContextMock;
	private readonly IMessageManager _messageManager;

	public MessageManagerTests()
	{
		_loggerMock = new Mock<ILogger<MessageManager>>();
		_memoryCacheMock = new Mock<IMemoryCache>();
		_messageRepositoryMock = new Mock<IMessageRepository>();
		_chatSessionRepositoryMock = new Mock<IChatSessionRepository>();
		_configurationMock = new Mock<IConfiguration>();
		_commandContextMock = new Mock<ICommandContext>();

		// Setup configuration
		_configurationMock.Setup(x => x.GetValue<int>(It.IsAny<string>()))
			.Returns(40);

		_messageManager = new MessageManager(
			_loggerMock.Object,
			_memoryCacheMock.Object,
			_messageRepositoryMock.Object,
			_chatSessionRepositoryMock.Object,
			_configurationMock.Object,
			_commandContextMock.Object);
	}

	[Fact]
	public void Constructor_WithNullCommandContext_ShouldThrowArgumentNullException()
	{
		// Act & Assert
		Assert.Throws<ArgumentNullException>(() => new MessageManager(
			_loggerMock.Object,
			_memoryCacheMock.Object,
			_messageRepositoryMock.Object,
			_chatSessionRepositoryMock.Object,
			_configurationMock.Object,
			null!));
	}

	[Fact]
	public void ClearMessageCache_ShouldRemoveCacheEntries()
	{
		// Arrange
		var cacheEntry = Mock.Of<ICacheEntry>();
		_memoryCacheMock.Setup(x => x.CreateEntry(It.IsAny<object>()))
			.Returns(cacheEntry);

		// Act
		_messageManager.ClearMessageCache();

		// Assert
		_memoryCacheMock.Verify(x => x.Remove("computed_persona_message"), Times.Once);
		_memoryCacheMock.Verify(x => x.Remove("core_persona_message"), Times.Once);
	}

	[Fact]
	public async Task GetChatSessionsAsync_WithValidInstanceId_ShouldReturnCachedSessions()
	{
		// Arrange
		const string instanceId = "test-instance";
		var expectedSessions = new List<ChatSession>
		{
			new() { Id = "session1", Name = "Test Session 1" },
			new() { Id = "session2", Name = "Test Session 2" }
		};

		var cacheEntry = Mock.Of<ICacheEntry>();
		_memoryCacheMock.Setup(x => x.CreateEntry(instanceId))
			.Returns(cacheEntry);

		// Setup GetOrCreateAsync to return expected sessions
		_memoryCacheMock.Setup(x => x.GetOrCreateAsync(instanceId, It.IsAny<Func<ICacheEntry, Task<List<ChatSession>>>>()))
			.ReturnsAsync(expectedSessions);

		// Act
		var result = await _messageManager.GetChatSessionsAsync(instanceId);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(2, result.Count);
		Assert.All(result, session => Assert.NotNull(session.Id));
	}

	[Fact]
	public async Task GetSessionAsync_WithValidSessionId_ShouldReturnSession()
	{
		// Arrange
		const string sessionId = "test-session";
		var expectedSession = new ChatSession
		{
			Id = sessionId,
			Name = "Test Session"
		};

		_chatSessionRepositoryMock.Setup(x => x.GetAsync(sessionId))
			.ReturnsAsync(expectedSession);

		// Act
		var result = await _messageManager.GetSessionAsync(sessionId);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(sessionId, result.SessionId);
		_chatSessionRepositoryMock.Verify(x => x.GetAsync(sessionId), Times.Once);
	}

	[Fact]
	public async Task GetSessionAsync_WithInvalidSessionId_ShouldReturnNull()
	{
		// Arrange
		const string sessionId = "invalid-session";

		_chatSessionRepositoryMock.Setup(x => x.GetAsync(sessionId))
			.ReturnsAsync((ChatSession?)null);

		// Act
		var result = await _messageManager.GetSessionAsync(sessionId);

		// Assert
		Assert.Null(result);
		_chatSessionRepositoryMock.Verify(x => x.GetAsync(sessionId), Times.Once);
	}

	[Fact]
	public async Task GetOrCreateChatSessionAsync_WithExistingSession_ShouldReturnExistingSession()
	{
		// Arrange
		const string sessionId = "existing-session";
		var existingSession = new ChatSession
		{
			Id = sessionId,
			Name = "Existing Session"
		};

		_chatSessionRepositoryMock.Setup(x => x.GetAsync(sessionId))
			.ReturnsAsync(existingSession);

		// Act
		var result = await _messageManager.GetOrCreateChatSessionAsync(sessionId);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(sessionId, result.SessionId);
		_chatSessionRepositoryMock.Verify(x => x.GetAsync(sessionId), Times.Once);
		_chatSessionRepositoryMock.Verify(x => x.AddAsync(It.IsAny<ChatSession>()), Times.Never);
	}

	[Fact]
	public async Task GetOrCreateChatSessionAsync_WithNewSession_ShouldCreateNewSession()
	{
		// Arrange
		const string sessionId = "new-session";

		_chatSessionRepositoryMock.Setup(x => x.GetAsync(sessionId))
			.ReturnsAsync((ChatSession?)null);

		_commandContextMock.Setup(x => x.InstanceId)
			.Returns("test-instance");

		_chatSessionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<ChatSession>()))
			.ReturnsAsync(true);

		// Act
		var result = await _messageManager.GetOrCreateChatSessionAsync(sessionId);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(sessionId, result.SessionId);
		Assert.Equal("test-instance", result.InstanceId);
		_chatSessionRepositoryMock.Verify(x => x.GetAsync(sessionId), Times.Once);
		_chatSessionRepositoryMock.Verify(x => x.AddAsync(It.Is<ChatSession>(s => s.Id == sessionId)), Times.Once);
	}

	[Fact]
	public async Task AddChatExchangeAsync_WithValidData_ShouldAddMessages()
	{
		// Arrange
		const string instanceId = "test-instance";
		var chatMessages = new List<ChatMessageWithMetadata>
		{
			new() { Message = UserChatMessage.CreateUserMessage("Test message 1") },
			new() { Message = AssistantChatMessage.CreateAssistantMessage("Test response 1") }
		};

		var modelMessages = new List<Message>
		{
			new() { Id = "msg1", Content = "Test message 1" },
			new() { Id = "msg2", Content = "Test response 1" }
		};

		var session = new ChatSession { Id = "session1", Name = "Test Session" };

		_commandContextMock.Setup(x => x.SessionId).Returns("session1");
		_chatSessionRepositoryMock.Setup(x => x.GetAsync("session1"))
			.ReturnsAsync(session);

		_messageRepositoryMock.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<Message>>()))
			.ReturnsAsync(true);

		// Act
		await _messageManager.AddChatExchangeAsync(instanceId, chatMessages, modelMessages);

		// Assert
		_messageRepositoryMock.Verify(x => x.AddRangeAsync(It.Is<IEnumerable<Message>>(msgs =>
			msgs.Count() == 2 &&
			msgs.All(m => m.SessionId == "session1"))), Times.Once);
	}

	[Theory]
	[InlineData("test-key", "Modified message", 30)]
	[InlineData("another-key", "Another message", 60)]
	public void ModifyMessage_WithValidParameters_ShouldSetCacheEntry(string key, string message, int minutes)
	{
		// Arrange
		var cacheEntry = Mock.Of<ICacheEntry>();
		_memoryCacheMock.Setup(x => x.CreateEntry(key))
			.Returns(cacheEntry);

		// Act
		_messageManager.ModifyMessage(key, message, minutes);

		// Assert
		_memoryCacheMock.Verify(x => x.CreateEntry(key), Times.Once);
		Assert.Equal(message, cacheEntry.Value);
		Assert.Equal(TimeSpan.FromMinutes(minutes), cacheEntry.AbsoluteExpirationRelativeToNow);
	}

	[Fact]
	public async Task GetPersonaCoreMessageAsync_ShouldReturnCachedMessage()
	{
		// Arrange
		const string expectedMessage = "Core persona message";

		_memoryCacheMock.Setup(x => x.GetOrCreateAsync(
			"core_persona_message",
			It.IsAny<Func<ICacheEntry, Task<string?>>>()))
			.ReturnsAsync(expectedMessage);

		// Act
		var result = await _messageManager.GetPersonaCoreMessageAsync();

		// Assert
		Assert.Equal(expectedMessage, result);
		_memoryCacheMock.Verify(x => x.GetOrCreateAsync(
			"core_persona_message",
			It.IsAny<Func<ICacheEntry, Task<string?>>>()), Times.Once);
	}

	[Theory]
	[InlineData("session1", 5)]
	[InlineData("session2", 0)]
	[InlineData("empty-session", 0)]
	public void GetChatMessageCount_WithDifferentSessions_ShouldReturnCorrectCount(string sessionId, int expectedCount)
	{
		// Arrange
		var messages = new List<Message>();
		for (int i = 0; i < expectedCount; i++)
		{
			messages.Add(new Message { Id = $"msg{i}", SessionId = sessionId });
		}

		var cacheKey = $"chat_messages_{sessionId}";
		_memoryCacheMock.Setup(x => x.TryGetValue(cacheKey, out It.Ref<object?>.IsAny))
			.Returns((object key, out object? value) =>
			{
				value = messages;
				return expectedCount > 0;
			});

		// Act
		var result = _messageManager.GetChatMessageCount(sessionId);

		// Assert
		Assert.Equal(expectedCount, result);
	}

	[Theory]
	[InlineData("session1", 3, 10)]
	[InlineData("session2", 0, 5)]
	[InlineData("session3", 1, 1)]
	public void ClearOldMessages_WithValidParameters_ShouldClearMessagesCorrectly(string sessionId, int initialCount, int range)
	{
		// Arrange
		var messages = new List<Message>();
		for (int i = 0; i < initialCount; i++)
		{
			messages.Add(new Message { Id = $"msg{i}", SessionId = sessionId });
		}

		var cacheKey = $"chat_messages_{sessionId}";
		var cacheEntry = Mock.Of<ICacheEntry>();

		_memoryCacheMock.Setup(x => x.TryGetValue(cacheKey, out It.Ref<object?>.IsAny))
			.Returns((object key, out object? value) =>
			{
				value = messages;
				return initialCount > 0;
			});

		_memoryCacheMock.Setup(x => x.CreateEntry(cacheKey))
			.Returns(cacheEntry);

		// Act
		_messageManager.ClearOldMessages(sessionId, range);

		// Assert
		if (initialCount > 0)
		{
			_memoryCacheMock.Verify(x => x.CreateEntry(cacheKey), Times.Once);

			// Verify that messages were cleared based on range
			var expectedRemainingCount = Math.Max(0, initialCount - range);
			var clearedMessages = (List<Message>)cacheEntry.Value!;
			Assert.Equal(expectedRemainingCount, clearedMessages.Count);
		}
	}
}
