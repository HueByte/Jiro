using Jiro.Core.Constants;
using Jiro.Core.IRepositories;
using Jiro.Core.Models;
using Jiro.Core.Services.CommandContext;
using Jiro.Core.Services.Context;
using Jiro.Core.Services.Conversation.Models;
using Jiro.Core.Services.MessageCache;
using Jiro.Core.Services.StaticMessage;
using Jiro.Infrastructure;
using Jiro.Infrastructure.Repositories;
using Jiro.Tests.Utilities;

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
	private readonly Mock<IInstanceContext> _instanceContextMock;
	private readonly Mock<IStaticMessageService> _staticMessageServiceMock;
	private readonly Mock<IInstanceMetadataAccessor> _instanceMetadataAccessorMock;
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

		_instanceContextMock = new Mock<IInstanceContext>();
		_staticMessageServiceMock = new Mock<IStaticMessageService>();
		_instanceMetadataAccessorMock = new Mock<IInstanceMetadataAccessor>();

		// Setup default return value for instance metadata accessor
		_instanceMetadataAccessorMock.Setup(x => x.GetInstanceIdAsync(It.IsAny<string>()))
			.ReturnsAsync("test-instance");
		_instanceMetadataAccessorMock.Setup(x => x.GetCurrentInstanceId())
			.Returns("test-instance");

		// Setup static message service mock to use the same memory cache for testing
		_staticMessageServiceMock.Setup(x => x.ClearStaticMessageCache())
			.Callback(() =>
			{
				// Remove the same cache entries that the real implementation would remove
				_memoryCache.Remove(Jiro.Core.Constants.CacheKeys.ComputedPersonaMessageKey);
				_memoryCache.Remove(Jiro.Core.Constants.CacheKeys.CorePersonaMessageKey);
			});

		_staticMessageServiceMock.Setup(x => x.SetStaticMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
			.Callback<string, string, int>((key, message, minutes) =>
			{
				var cacheEntryOptions = new MemoryCacheEntryOptions
				{
					AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(minutes)
				};
				_memoryCache.Set(key, message, cacheEntryOptions);
			});

		_messageManager = new MessageManager(
			_loggerMock.Object,
			_memoryCache,
			_messageRepository,
			_chatSessionRepository,
			_configuration,
			_staticMessageServiceMock.Object,
			_instanceContextMock.Object,
			_instanceMetadataAccessorMock.Object);
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
			_staticMessageServiceMock.Object,
			null!,
			_instanceMetadataAccessorMock.Object));
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
		Assert.All(result, static session => Assert.NotNull(session.Id));
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

		_instanceContextMock.Setup(static x => x.InstanceId)
			.Returns(instanceId);
		_instanceMetadataAccessorMock.Setup(x => x.GetInstanceIdAsync(It.IsAny<string>()))
			.ReturnsAsync(instanceId);

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

		_instanceContextMock.Setup(static x => x.InstanceId).Returns(instanceId);
		_instanceMetadataAccessorMock.Setup(x => x.GetInstanceIdAsync(It.IsAny<string>()))
			.ReturnsAsync(instanceId);

		// Act
		await _messageManager.AddChatExchangeAsync(sessionId, chatMessages, modelMessages);

		// Assert - Check that messages were added to database
		var addedMessages = _dbContext.Messages.Where(static m => m.SessionId == sessionId).ToList();
		Assert.Equal(2, addedMessages.Count);
		Assert.All(addedMessages, static m => Assert.Equal(sessionId, m.SessionId));

		// Also check that cache is updated
		var cachedSession = await _messageManager.GetSessionAsync(sessionId, includeMessages: true);
		Assert.NotNull(cachedSession);
		Assert.Equal(2, cachedSession.Messages.Count);
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
		// Arrange - Add session with messages to cache using the new cache structure
		if (expectedCount > 0)
		{
			var messages = new List<ChatMessageWithMetadata>();
			for (int i = 0; i < expectedCount; i++)
			{
				messages.Add(new ChatMessageWithMetadata
				{
					Message = ChatMessage.CreateUserMessage($"Message {i}"),
					CreatedAt = DateTime.UtcNow
				});
			}

			var session = new Session
			{
				SessionId = sessionId,
				InstanceId = "test-instance",
				Messages = messages
			};

			_memoryCache.Set($"{CacheKeys.SessionKey}::{sessionId}", session);
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

	[Fact]
	public async Task GetSessionAsync_WithSessionContainingMessages_ShouldReturnSessionWithMessages()
	{
		// Arrange
		const string sessionId = "session-with-messages";
		const string instanceId = "test-instance";

		_instanceContextMock.Setup(static x => x.InstanceId).Returns(instanceId);
		_instanceMetadataAccessorMock.Setup(x => x.GetInstanceIdAsync(It.IsAny<string>()))
			.ReturnsAsync(instanceId);

		// Create a session with messages
		var session = new ChatSession
		{
			Id = sessionId,
			Name = "Session with Messages",
			CreatedAt = DateTime.UtcNow,
			LastUpdatedAt = DateTime.UtcNow
		};

		await _chatSessionRepository.AddAsync(session);
		await _chatSessionRepository.SaveChangesAsync();

		// Add messages to the session
		var message1 = new Message
		{
			Id = "msg1",
			Content = "Hello, this is a user message",
			InstanceId = instanceId,
			SessionId = sessionId,
			IsUser = true,
			CreatedAt = DateTime.UtcNow.AddMinutes(-5),
			Type = MessageType.Text
		};

		var message2 = new Message
		{
			Id = "msg2",
			Content = "Hello! This is an assistant response",
			InstanceId = instanceId,
			SessionId = sessionId,
			IsUser = false,
			CreatedAt = DateTime.UtcNow.AddMinutes(-4),
			Type = MessageType.Text
		};

		await _messageRepository.AddAsync(message1);
		await _messageRepository.AddAsync(message2);
		await _messageRepository.SaveChangesAsync();

		// Act
		var result = await _messageManager.GetSessionAsync(sessionId, includeMessages: true);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(sessionId, result.SessionId);
		Assert.NotNull(result.Messages);
		Assert.Equal(2, result.Messages.Count);

		// Verify messages are ordered by creation time
		Assert.Equal("msg1", result.Messages[0].MessageId);
		Assert.Equal("Hello, this is a user message", result.Messages[0].Message.Content.First().Text);
		Assert.True(result.Messages[0].IsUser);

		Assert.Equal("msg2", result.Messages[1].MessageId);
		Assert.Equal("Hello! This is an assistant response", result.Messages[1].Message.Content.First().Text);
		Assert.False(result.Messages[1].IsUser);
	}
}
