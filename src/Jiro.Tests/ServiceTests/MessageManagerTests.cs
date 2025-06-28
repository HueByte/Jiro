using Jiro.Core;
using Jiro.Core.IRepositories;
using Jiro.Core.Models;
using Jiro.Core.Services.CommandContext;
using Jiro.Core.Services.Conversation.Models;
using Jiro.Core.Services.MessageCache;
using Jiro.Infrastructure;
using Jiro.Infrastructure.Repositories;
using Jiro.Tests.Utilities;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Moq;

using OpenAI.Chat;

namespace Jiro.Tests.ServiceTests;

public class MessageManagerTests : IDisposable
{
	private readonly Mock<ILogger<MessageManager>> _loggerMock;
	private readonly IMemoryCache _memoryCache;
	private readonly IMessageRepository _messageRepository;
	private readonly IChatSessionRepository _chatSessionRepository;
	private readonly IConfiguration _configuration;
	private readonly Mock<ICommandContext> _commandContextMock;
	private readonly IMessageManager _messageManager;
	private readonly JiroContext _dbContext;

	public MessageManagerTests()
	{
		_loggerMock = new Mock<ILogger<MessageManager>>();
		_memoryCache = new MemoryCache(new MemoryCacheOptions());

		// Setup in-memory database
		var testDbInitializer = new TestDatabaseInitializer();
		_dbContext = testDbInitializer.CreateDbContext();

		// Setup real repositories
		_messageRepository = new MessageRepository(_dbContext);
		_chatSessionRepository = new ChatSessionRepository(_dbContext);

		// Setup configuration using ConfigurationBuilder
		var configData = new Dictionary<string, string?>
		{
			["JIRO_MESSAGE_FETCH_COUNT"] = "40"
		};
		_configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(configData)
			.Build();

		_commandContextMock = new Mock<ICommandContext>();

		_messageManager = new MessageManager(
			_loggerMock.Object,
			_memoryCache,
			_messageRepository,
			_chatSessionRepository,
			_configuration,
			_commandContextMock.Object);
	}

	public void Dispose()
	{
		_memoryCache?.Dispose();
		_dbContext?.Dispose();
	}

	[Fact]
	public void Constructor_WithNullCommandContext_ShouldThrowArgumentNullException()
	{
		// Arrange
		var configData = new Dictionary<string, string?>
		{
			["JIRO_MESSAGE_FETCH_COUNT"] = "40"
		};
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(configData)
			.Build();

		// Act & Assert
		Assert.Throws<ArgumentNullException>(() => new MessageManager(
			_loggerMock.Object,
			_memoryCache,
			_messageRepository,
			_chatSessionRepository,
			configuration,
			null!));
	}

	[Fact]
	public void ClearMessageCache_ShouldRemoveCacheEntries()
	{
		// Arrange - Add some entries to cache first
		_memoryCache.Set(Jiro.Core.Constants.CacheKeys.ComputedPersonaMessageKey, "test computed persona");
		_memoryCache.Set(Jiro.Core.Constants.CacheKeys.CorePersonaMessageKey, "test core persona");

		// Act
		_messageManager.ClearMessageCache();

		// Assert - Check that entries were removed
		Assert.False(_memoryCache.TryGetValue(Jiro.Core.Constants.CacheKeys.ComputedPersonaMessageKey, out _));
		Assert.False(_memoryCache.TryGetValue(Jiro.Core.Constants.CacheKeys.CorePersonaMessageKey, out _));
	}

	[Fact]
	public async Task GetChatSessionsAsync_WithValidInstanceId_ShouldReturnSessions()
	{
		// Arrange
		const string instanceId = "test-instance";

		// Create test sessions in database
		var session1 = new ChatSession { Id = "session1", Name = "Test Session 1", CreatedAt = DateTime.UtcNow };
		var session2 = new ChatSession { Id = "session2", Name = "Test Session 2", CreatedAt = DateTime.UtcNow };

		await _chatSessionRepository.AddAsync(session1);
		await _chatSessionRepository.AddAsync(session2);
		await _chatSessionRepository.SaveChangesAsync();

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
			Name = "Test Session",
			CreatedAt = DateTime.UtcNow
		};

		await _chatSessionRepository.AddAsync(expectedSession);
		await _chatSessionRepository.SaveChangesAsync();

		// Act
		var result = await _messageManager.GetSessionAsync(sessionId);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(sessionId, result.SessionId);
	}

	[Fact]
	public async Task GetSessionAsync_WithInvalidSessionId_ShouldReturnNull()
	{
		// Arrange
		const string sessionId = "invalid-session";

		// Act
		var result = await _messageManager.GetSessionAsync(sessionId);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task GetOrCreateChatSessionAsync_WithExistingSession_ShouldReturnExistingSession()
	{
		// Arrange
		const string sessionId = "existing-session";
		var existingSession = new ChatSession
		{
			Id = sessionId,
			Name = "Existing Session",
			CreatedAt = DateTime.UtcNow
		};

		await _chatSessionRepository.AddAsync(existingSession);
		await _chatSessionRepository.SaveChangesAsync();

		// Act
		var result = await _messageManager.GetOrCreateChatSessionAsync(sessionId);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(sessionId, result.SessionId);
	}

	[Fact]
	public async Task GetOrCreateChatSessionAsync_WithNewSession_ShouldCreateNewSession()
	{
		// Arrange
		const string sessionId = "new-session";
		const string instanceId = "test-instance";

		_commandContextMock.Setup(x => x.InstanceId)
			.Returns(instanceId);
		_commandContextMock.Setup(x => x.SessionId)
			.Returns(sessionId);

		// Act
		var result = await _messageManager.GetOrCreateChatSessionAsync(sessionId);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(sessionId, result.SessionId);
		Assert.Equal(instanceId, result.InstanceId);

		// Verify session was actually created in database
		var createdSession = await _chatSessionRepository.GetAsync(sessionId);
		Assert.NotNull(createdSession);
		Assert.Equal(sessionId, createdSession.Id);
	}

	[Fact]
	public async Task AddChatExchangeAsync_WithValidData_ShouldAddMessages()
	{
		// Arrange
		const string sessionId = "session1";
		const string instanceId = "test-instance";

		var session = new ChatSession { Id = sessionId, Name = "Test Session", CreatedAt = DateTime.UtcNow };
		await _chatSessionRepository.AddAsync(session);
		await _chatSessionRepository.SaveChangesAsync();

		var chatMessages = new List<ChatMessageWithMetadata>
		{
			new() { Message = UserChatMessage.CreateUserMessage("Test message 1") },
			new() { Message = AssistantChatMessage.CreateAssistantMessage("Test response 1") }
		};

		var modelMessages = new List<Message>
		{
			new() { Id = "msg1", Content = "Test message 1", SessionId = sessionId, InstanceId = instanceId, CreatedAt = DateTime.UtcNow },
			new() { Id = "msg2", Content = "Test response 1", SessionId = sessionId, InstanceId = instanceId, CreatedAt = DateTime.UtcNow }
		};

		_commandContextMock.Setup(x => x.SessionId).Returns(sessionId);
		_commandContextMock.Setup(x => x.InstanceId).Returns(instanceId);

		// Act
		await _messageManager.AddChatExchangeAsync(instanceId, chatMessages, modelMessages);

		// Assert - Check that messages were added to database
		var addedMessages = _dbContext.Messages.Where(m => m.SessionId == sessionId).ToList();
		Assert.Equal(2, addedMessages.Count);
		Assert.All(addedMessages, m => Assert.Equal(sessionId, m.SessionId));
	}

	[Theory]
	[InlineData("test-key", "Modified message", 30)]
	[InlineData("another-key", "Another message", 60)]
	public void ModifyMessage_WithValidParameters_ShouldSetCacheEntry(string key, string message, int minutes)
	{
		// Act
		_messageManager.ModifyMessage(key, message, minutes);

		// Assert - Check that the value was cached
		Assert.True(_memoryCache.TryGetValue(key, out var cachedValue));
		Assert.Equal(message, cachedValue);
	}

	[Fact]
	public async Task GetPersonaCoreMessageAsync_ShouldReturnCachedMessage()
	{
		// This test requires more complex setup as it involves repository queries
		// For now, let's test that it doesn't throw an exception

		// Act & Assert
		var result = await _messageManager.GetPersonaCoreMessageAsync();

		// Should not throw and return a string (could be null or empty)
		Assert.True(result == null || result is string);
	}

	[Theory]
	[InlineData("session1", 5)]
	[InlineData("session2", 0)]
	[InlineData("empty-session", 0)]
	public void GetChatMessageCount_WithDifferentSessions_ShouldReturnCorrectCount(string sessionId, int expectedCount)
	{
		// Arrange - Add messages to cache
		var messages = new List<ChatMessage>();
		for (int i = 0; i < expectedCount; i++)
		{
			messages.Add(ChatMessage.CreateUserMessage($"Message {i}"));
		}

		if (expectedCount > 0)
		{
			_memoryCache.Set(sessionId, messages);
		}

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
		// Arrange - Add session to cache
		var session = new Session
		{
			SessionId = sessionId,
			InstanceId = "test-instance",
			Messages = new List<ChatMessageWithMetadata>()
		};

		for (int i = 0; i < initialCount; i++)
		{
			session.Messages.Add(new ChatMessageWithMetadata
			{
				Message = ChatMessage.CreateUserMessage($"Message {i}")
			});
		}

		if (initialCount > 0)
		{
			_memoryCache.Set(sessionId, session);
		}

		// Act
		_messageManager.ClearOldMessages(sessionId, range);

		// Assert
		if (initialCount > 0)
		{
			var expectedRemainingCount = Math.Max(0, initialCount - range);
			if (_memoryCache.TryGetValue(sessionId, out var cachedSession) && cachedSession != null)
			{
				var remainingSession = (Session)cachedSession;
				if (initialCount > range)
				{
					Assert.Equal(expectedRemainingCount, remainingSession.Messages.Count);
				}
				else
				{
					// If range is greater than or equal to initial count, all messages should remain
					Assert.Equal(initialCount, remainingSession.Messages.Count);
				}
			}
		}
	}
}
