using Jiro.Core.Services.Conversation;
using Jiro.Core.Services.MessageCache;
using Jiro.Core.Services.Persona;
using Jiro.Core.Services.Semaphore;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Moq;

namespace Jiro.Tests.ServiceTests;

public class PersonaServiceTests
{
    private readonly Mock<ILogger<PersonaService>> _loggerMock;
    private readonly Mock<IMessageManager> _messageManagerMock;
    private readonly Mock<IConversationCoreService> _conversationCoreMock;
    private readonly Mock<IMemoryCache> _memoryCacheMock;
    private readonly Mock<ISemaphoreManager> _semaphoreManagerMock;
    private readonly IPersonaService _personaService;
    private readonly Mock<SemaphoreSlim> _semaphoreMock;

    public PersonaServiceTests ()
    {
        _loggerMock = new Mock<ILogger<PersonaService>>();
        _messageManagerMock = new Mock<IMessageManager>();
        _conversationCoreMock = new Mock<IConversationCoreService>();
        _memoryCacheMock = new Mock<IMemoryCache>();
        _semaphoreManagerMock = new Mock<ISemaphoreManager>();

        _semaphoreMock = new Mock<SemaphoreSlim>(1, 1);
        _semaphoreManagerMock.Setup(x => x.GetOrCreateInstanceSemaphore(It.IsAny<string>()))
            .Returns(_semaphoreMock.Object);

        _personaService = new PersonaService(
            _loggerMock.Object,
            _messageManagerMock.Object,
            _conversationCoreMock.Object,
            _memoryCacheMock.Object,
            _semaphoreManagerMock.Object);
    }

    [Fact]
    public async Task GetPersonaAsync_WithEmptyInstanceId_ShouldReturnDefaultPersona ()
    {
        // Arrange
        const string expectedPersona = "Default persona message";

        _messageManagerMock.Setup(x => x.GetPersonaCoreMessageAsync())
            .ReturnsAsync(expectedPersona);

        // Act
        var result = await _personaService.GetPersonaAsync("");

        // Assert
        Assert.Equal(expectedPersona, result);
        _messageManagerMock.Verify(x => x.GetPersonaCoreMessageAsync(), Times.Once);

        // Verify warning was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Instance ID is empty")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPersonaAsync_WithValidInstanceId_ShouldUseSemaphoreAndReturnPersona ()
    {
        // Arrange
        const string instanceId = "test-instance";
        const string expectedPersona = "Instance persona message";

        _messageManagerMock.Setup(x => x.GetPersonaCoreMessageAsync())
            .ReturnsAsync(expectedPersona);

        // Act
        var result = await _personaService.GetPersonaAsync(instanceId);

        // Assert
        Assert.Equal(expectedPersona, result);
        _semaphoreManagerMock.Verify(x => x.GetOrCreateInstanceSemaphore(instanceId), Times.Once);
        _semaphoreMock.Verify(x => x.WaitAsync(), Times.Once);
        _semaphoreMock.Verify(x => x.Release(), Times.Once);
        _messageManagerMock.Verify(x => x.GetPersonaCoreMessageAsync(), Times.Once);
    }

    [Fact]
    public async Task GetPersonaAsync_WhenExceptionOccurs_ShouldReleaseSemaphoreAndRethrow ()
    {
        // Arrange
        const string instanceId = "test-instance";
        var expectedException = new InvalidOperationException("Test exception");

        _messageManagerMock.Setup(x => x.GetPersonaCoreMessageAsync())
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _personaService.GetPersonaAsync(instanceId));

        Assert.Equal(expectedException.Message, exception.Message);
        _semaphoreMock.Verify(x => x.WaitAsync(), Times.Once);
        _semaphoreMock.Verify(x => x.Release(), Times.Once);

        // Verify error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error retrieving persona")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AddSummaryAsync_WithCachedPersona_ShouldUpdateCacheWithSummary ()
    {
        // Arrange
        const string updateMessage = "Recent conversation summary";
        const string cachedPersona = "Existing persona";
        const string expectedUpdatedPersona = $"{cachedPersona}\nThis is your summary of recent conversations: {updateMessage}";

        _memoryCacheMock.Setup(x => x.Get<string>("computed_persona_message"))
            .Returns(cachedPersona);
        _memoryCacheMock.Setup(x => x.Set("computed_persona_message", expectedUpdatedPersona, TimeSpan.FromDays(1)));

        // Act
        await _personaService.AddSummaryAsync(updateMessage);

        // Assert
        _memoryCacheMock.Verify(x => x.Get<string>("computed_persona_message"), Times.Once);
        _memoryCacheMock.Verify(x => x.Set("computed_persona_message", expectedUpdatedPersona, TimeSpan.FromDays(1)), Times.Once);

        // Verify info was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Persona summary updated")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AddSummaryAsync_WithoutCachedPersona_ShouldLoadFromSourceAndUpdate ()
    {
        // Arrange
        const string updateMessage = "Recent conversation summary";
        const string personaFromSource = "Persona from source";
        const string expectedUpdatedPersona = $"{personaFromSource}\nThis is your summary of recent conversations: {updateMessage}";

        _memoryCacheMock.Setup(x => x.Get<string>("computed_persona_message"))
            .Returns((string?)null);
        _messageManagerMock.Setup(x => x.GetPersonaCoreMessageAsync())
            .ReturnsAsync(personaFromSource);
        _memoryCacheMock.Setup(x => x.Set("computed_persona_message", expectedUpdatedPersona, TimeSpan.FromDays(1)));

        // Act
        await _personaService.AddSummaryAsync(updateMessage);

        // Assert
        _memoryCacheMock.Verify(x => x.Get<string>("computed_persona_message"), Times.Once);
        _messageManagerMock.Verify(x => x.GetPersonaCoreMessageAsync(), Times.Once);
        _memoryCacheMock.Verify(x => x.Set("computed_persona_message", expectedUpdatedPersona, TimeSpan.FromDays(1)), Times.Once);

        // Verify both info logs
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Persona message cache miss")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Persona summary updated")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AddSummaryAsync_WhenExceptionOccurs_ShouldLogErrorAndRethrow ()
    {
        // Arrange
        const string updateMessage = "Recent conversation summary";
        var expectedException = new InvalidOperationException("Test exception");

        _memoryCacheMock.Setup(x => x.Get<string>("computed_persona_message"))
            .Throws(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _personaService.AddSummaryAsync(updateMessage));

        Assert.Equal(expectedException.Message, exception.Message);

        // Verify error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error updating persona summary")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task AddSummaryAsync_WithEmptyUpdateMessage_ShouldStillUpdatePersona (string? updateMessage)
    {
        // Arrange
        const string cachedPersona = "Existing persona";
        var expectedUpdatedPersona = $"{cachedPersona}\nThis is your summary of recent conversations: {updateMessage}";

        _memoryCacheMock.Setup(x => x.Get<string>("computed_persona_message"))
            .Returns(cachedPersona);
        _memoryCacheMock.Setup(x => x.Set("computed_persona_message", expectedUpdatedPersona, TimeSpan.FromDays(1)));

        // Act
        await _personaService.AddSummaryAsync(updateMessage!);

        // Assert
        _memoryCacheMock.Verify(x => x.Set("computed_persona_message", expectedUpdatedPersona, TimeSpan.FromDays(1)), Times.Once);
    }
}
