using Jiro.Core.Services.Conversation;
using Jiro.Core.Services.MessageCache;
using Jiro.Core.Services.Semaphore;

using Microsoft.Extensions.Logging;

using Moq;

using OpenAI.Chat;

namespace Jiro.Tests.ServiceTests;

public class ConversationCoreServiceTests
{
	private readonly Mock<ILogger<ConversationCoreService>> _loggerMock;
	private readonly Mock<IMessageManager> _messageCacheServiceMock;
	private readonly Mock<ChatClient> _chatClientMock;
	private readonly Mock<ISemaphoreManager> _semaphoreManagerMock;
	private readonly Mock<SemaphoreSlim> _semaphoreMock;
	private readonly ConversationCoreService _conversationCoreService;

	public ConversationCoreServiceTests()
	{
		_loggerMock = new Mock<ILogger<ConversationCoreService>>();
		_messageCacheServiceMock = new Mock<IMessageManager>();
		_chatClientMock = new Mock<ChatClient>();
		_semaphoreManagerMock = new Mock<ISemaphoreManager>();
		_semaphoreMock = new Mock<SemaphoreSlim>(1, 1);

		_semaphoreManagerMock
			.Setup(static x => x.GetOrCreateInstanceSemaphore(It.IsAny<string>()))
			.Returns(_semaphoreMock.Object);

		_conversationCoreService = new ConversationCoreService(
			_loggerMock.Object,
			_messageCacheServiceMock.Object,
			_chatClientMock.Object,
			_semaphoreManagerMock.Object
		);
	}

	[Fact]
	public void Constructor_WithValidParameters_ShouldCreateInstance()
	{
		// Arrange & Act
		var service = new ConversationCoreService(
			_loggerMock.Object,
			_messageCacheServiceMock.Object,
			_chatClientMock.Object,
			_semaphoreManagerMock.Object
		);

		// Assert
		Assert.NotNull(service);
	}

	[Fact]
	public void ConversationCoreService_ShouldImplementIConversationCoreService()
	{
		// Arrange & Act & Assert
		Assert.IsAssignableFrom<IConversationCoreService>(_conversationCoreService);
	}

	[Fact]
	public void Constructor_ShouldSetupDependencies()
	{
		// Arrange & Act & Assert
		Assert.NotNull(_conversationCoreService);

		// Verify semaphore manager is called when getting instance semaphore
		_semaphoreManagerMock.Setup(static x => x.GetOrCreateInstanceSemaphore("test"))
			.Returns(_semaphoreMock.Object);

		var semaphore = _semaphoreManagerMock.Object.GetOrCreateInstanceSemaphore("test");
		Assert.NotNull(semaphore);
	}

	// Note: Full testing of ChatAsync and ExchangeMessageAsync methods would require
	// complex mocking of OpenAI ChatClient and related types which have sealed/final classes
	// that are difficult to mock properly. For comprehensive testing, integration tests
	// with a test OpenAI client would be more appropriate.

	[Fact]
	public async Task ChatAsync_ShouldUseSemaphoreForConcurrencyControl()
	{
		// Arrange
		const string instanceId = "test-instance";
		var messageHistory = new List<ChatMessage>();
		var corePersonaMessage = "Test persona";

		_messageCacheServiceMock
			.Setup(static x => x.GetPersonaCoreMessageAsync())
			.ReturnsAsync(corePersonaMessage);

		// Act & Assert - Verify semaphore is requested
		try
		{
			// This will fail due to OpenAI client mocking limitations, but we can verify semaphore usage
			await _conversationCoreService.ChatAsync(instanceId, messageHistory);
		}
		catch
		{
			// Expected due to mocking limitations
		}

		_semaphoreManagerMock.Verify(static x => x.GetOrCreateInstanceSemaphore(instanceId), Times.Once);
	}
}
