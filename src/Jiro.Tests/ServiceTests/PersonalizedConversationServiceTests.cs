using Jiro.Core.Services.CommandContext;
using Jiro.Core.Services.Conversation;
using Jiro.Core.Services.MessageCache;
using Jiro.Core.Services.Persona;

using Microsoft.Extensions.Logging;

using Moq;

using OpenAI.Chat;

namespace Jiro.Tests.ServiceTests;

public class PersonalizedConversationServiceTests
{
	private readonly Mock<ILogger<PersonalizedConversationService>> _loggerMock;
	private readonly Mock<IConversationCoreService> _chatCoreServiceMock;
	private readonly Mock<IPersonaService> _personaServiceMock;
	private readonly Mock<IMessageManager> _messageCacheServiceMock;
	private readonly Mock<IHistoryOptimizerService> _historyOptimizerServiceMock;
	private readonly Mock<ICommandContext> _commandContextMock;
	private readonly PersonalizedConversationService _personalizedConversationService;

	public PersonalizedConversationServiceTests()
	{
		_loggerMock = new Mock<ILogger<PersonalizedConversationService>>();
		_chatCoreServiceMock = new Mock<IConversationCoreService>();
		_personaServiceMock = new Mock<IPersonaService>();
		_messageCacheServiceMock = new Mock<IMessageManager>();
		_historyOptimizerServiceMock = new Mock<IHistoryOptimizerService>();
		_commandContextMock = new Mock<ICommandContext>();

		_personalizedConversationService = new PersonalizedConversationService(
			_loggerMock.Object,
			_chatCoreServiceMock.Object,
			_personaServiceMock.Object,
			_messageCacheServiceMock.Object,
			_historyOptimizerServiceMock.Object,
			_commandContextMock.Object
		);
	}

	[Fact]
	public void Constructor_WithValidParameters_ShouldCreateInstance()
	{
		// Arrange & Act
		var service = new PersonalizedConversationService(
			_loggerMock.Object,
			_chatCoreServiceMock.Object,
			_personaServiceMock.Object,
			_messageCacheServiceMock.Object,
			_historyOptimizerServiceMock.Object,
			_commandContextMock.Object
		);

		// Assert
		Assert.NotNull(service);
	}

	[Fact]
	public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
	{
		// Arrange & Act & Assert
		Assert.Throws<ArgumentNullException>(() => new PersonalizedConversationService(
			null!,
			_chatCoreServiceMock.Object,
			_personaServiceMock.Object,
			_messageCacheServiceMock.Object,
			_historyOptimizerServiceMock.Object,
			_commandContextMock.Object
		));
	}

	[Fact]
	public void Constructor_WithNullChatCoreService_ShouldThrowArgumentNullException()
	{
		// Arrange & Act & Assert
		Assert.Throws<ArgumentNullException>(() => new PersonalizedConversationService(
			_loggerMock.Object,
			null!,
			_personaServiceMock.Object,
			_messageCacheServiceMock.Object,
			_historyOptimizerServiceMock.Object,
			_commandContextMock.Object
		));
	}

	[Fact]
	public void PersonalizedConversationService_ShouldImplementInterface()
	{
		// Arrange & Act & Assert
		Assert.IsAssignableFrom<IPersonalizedConversationService>(_personalizedConversationService);
	}

	[Fact]
	public async Task ExchangeMessageAsync_WithValidMessage_ShouldReturnResponse()
	{
		// Arrange
		const string inputMessage = "Hello, how are you?";
		const string expectedResponse = "I'm doing well, thank you!";
		const string personaContent = "You are Jiro";

		_personaServiceMock
			.Setup(x => x.GetPersonaAsync(string.Empty))
			.ReturnsAsync(personaContent);

		_chatCoreServiceMock
			.Setup(x => x.ExchangeMessageAsync(inputMessage, It.IsAny<ChatMessage>(), 1200))
			.ReturnsAsync(expectedResponse);

		// Act
		var result = await _personalizedConversationService.ExchangeMessageAsync(inputMessage);

		// Assert
		Assert.Equal(expectedResponse, result);
		_personaServiceMock.Verify(x => x.GetPersonaAsync(string.Empty), Times.Once);
	}

	// Note: ChatAsync method testing is complex due to OpenAI Chat types and complex dependencies.
	// The method involves ChatCompletion, ChatTokenUsage, and other sealed OpenAI types that are
	// difficult to mock properly. For comprehensive testing, integration tests would be more appropriate.

	[Theory]
	[InlineData("")]
	[InlineData("Simple message")]
	[InlineData("Complex message with special characters: !@#$%")]
	public async Task ExchangeMessageAsync_WithVariousMessages_ShouldHandleCorrectly(string message)
	{
		// Arrange
		const string expectedResponse = "Response";
		const string personaContent = "You are Jiro";

		_personaServiceMock
			.Setup(x => x.GetPersonaAsync(string.Empty))
			.ReturnsAsync(personaContent);

		_chatCoreServiceMock
			.Setup(x => x.ExchangeMessageAsync(message, It.IsAny<ChatMessage>(), 1200))
			.ReturnsAsync(expectedResponse);

		// Act
		var result = await _personalizedConversationService.ExchangeMessageAsync(message);

		// Assert
		Assert.Equal(expectedResponse, result);
	}
}
