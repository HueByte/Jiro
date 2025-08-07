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
	private readonly IMemoryCache _memoryCache; // Use real implementation
	private readonly Mock<ISemaphoreManager> _semaphoreManagerMock;
	private readonly IPersonaService _personaService;

	public PersonaServiceTests()
	{
		_loggerMock = new Mock<ILogger<PersonaService>>();
		_messageManagerMock = new Mock<IMessageManager>();
		_conversationCoreMock = new Mock<IConversationCoreService>();
		_memoryCache = new MemoryCache(new MemoryCacheOptions()); // Real implementation
		_semaphoreManagerMock = new Mock<ISemaphoreManager>();

		// Setup semaphore to return a real SemaphoreSlim instead of mocking it
		_semaphoreManagerMock.Setup(static x => x.GetOrCreateInstanceSemaphore(It.IsAny<string>()))
			.Returns(new SemaphoreSlim(1, 1));

		_personaService = new PersonaService(
			_loggerMock.Object,
			_messageManagerMock.Object,
			_conversationCoreMock.Object,
			_memoryCache,
			_semaphoreManagerMock.Object);
	}

	[Fact]
	public async Task GetPersonaAsync_WithEmptyInstanceId_ShouldReturnDefaultPersona()
	{
		// Arrange
		const string expectedPersona = "Default persona message";

		_messageManagerMock.Setup(static x => x.GetPersonaCoreMessageAsync())
			.ReturnsAsync(expectedPersona);

		// Act
		var result = await _personaService.GetPersonaAsync("");

		// Assert
		Assert.Equal(expectedPersona, result);
		_messageManagerMock.Verify(static x => x.GetPersonaCoreMessageAsync(), Times.Once);

		// Verify warning was logged
		_loggerMock.Verify(
			static x => x.Log(
				LogLevel.Warning,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>(static (v, t) => v.ToString()!.Contains("Instance ID is empty")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	[Fact]
	public async Task GetPersonaAsync_WithValidInstanceId_ShouldUseSemaphoreAndReturnPersona()
	{
		// Arrange
		const string instanceId = "test-instance";
		const string expectedPersona = "Instance persona message";

		_messageManagerMock.Setup(static x => x.GetPersonaCoreMessageAsync())
			.ReturnsAsync(expectedPersona);

		// Act
		var result = await _personaService.GetPersonaAsync(instanceId);

		// Assert
		Assert.Equal(expectedPersona, result);
		_semaphoreManagerMock.Verify(static x => x.GetOrCreateInstanceSemaphore(instanceId), Times.Once);
		_messageManagerMock.Verify(static x => x.GetPersonaCoreMessageAsync(), Times.Once);

		// Verify the persona was cached using the correct constant
		Assert.True(_memoryCache.TryGetValue(Jiro.Core.Constants.CacheKeys.ComputedPersonaMessageKey, out var cachedValue));
		Assert.Equal(expectedPersona, cachedValue);
	}

	[Fact]
	public async Task GetPersonaAsync_WhenExceptionOccurs_ShouldReleaseSemaphoreAndRethrow()
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

		// Verify that semaphore manager was called to get the semaphore
		_semaphoreManagerMock.Verify(x => x.GetOrCreateInstanceSemaphore(instanceId), Times.Once);

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
	public async Task AddSummaryAsync_WithCachedPersona_ShouldUpdateCacheWithSummary()
	{
		// Arrange
		const string updateMessage = "Recent conversation summary";
		const string cachedPersona = "Existing persona";
		const string expectedUpdatedPersona = $"{cachedPersona}\nThis is your summary of recent conversations: {updateMessage}";

		// Pre-populate cache with existing persona
		_memoryCache.Set(Jiro.Core.Constants.CacheKeys.ComputedPersonaMessageKey, cachedPersona);

		// Act
		await _personaService.AddSummaryAsync(updateMessage);

		// Assert - Check that the cache was updated with the summary
		Assert.True(_memoryCache.TryGetValue(Jiro.Core.Constants.CacheKeys.ComputedPersonaMessageKey, out var updatedValue));
		Assert.Equal(expectedUpdatedPersona, updatedValue);

		// Verify info was logged
		_loggerMock.Verify(
			static x => x.Log(
				LogLevel.Information,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>(static (v, t) => v.ToString()!.Contains("Persona summary updated")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	[Fact]
	public async Task AddSummaryAsync_WithoutCachedPersona_ShouldLoadFromSourceAndUpdate()
	{
		// Arrange
		const string updateMessage = "Recent conversation summary";
		const string personaFromSource = "Persona from source";
		const string expectedUpdatedPersona = $"{personaFromSource}\nThis is your summary of recent conversations: {updateMessage}";

		// Don't put anything in cache, so it will load from source
		_messageManagerMock.Setup(static x => x.GetPersonaCoreMessageAsync())
			.ReturnsAsync(personaFromSource);

		// Act
		await _personaService.AddSummaryAsync(updateMessage);

		// Assert
		_messageManagerMock.Verify(static x => x.GetPersonaCoreMessageAsync(), Times.Once);

		// Verify the cache was updated with the combined message
		Assert.True(_memoryCache.TryGetValue(Jiro.Core.Constants.CacheKeys.ComputedPersonaMessageKey, out var updatedValue));
		Assert.Equal(expectedUpdatedPersona, updatedValue);

		// Verify both info logs
		_loggerMock.Verify(
			static x => x.Log(
				LogLevel.Information,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>(static (v, t) => v.ToString()!.Contains("Persona message cache miss")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);

		_loggerMock.Verify(
			static x => x.Log(
				LogLevel.Information,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>(static (v, t) => v.ToString()!.Contains("Persona summary updated")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	[Fact]
	public async Task AddSummaryAsync_WhenExceptionOccurs_ShouldLogErrorAndRethrow()
	{
		// Arrange
		const string updateMessage = "Recent conversation summary";
		var expectedException = new InvalidOperationException("Test exception");

		// Setup the message manager to throw when called
		_messageManagerMock.Setup(x => x.GetPersonaCoreMessageAsync())
			.ThrowsAsync(expectedException);

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
	public async Task AddSummaryAsync_WithEmptyUpdateMessage_ShouldStillUpdatePersona(string? updateMessage)
	{
		// Arrange
		const string cachedPersona = "Existing persona";
		var expectedUpdatedPersona = $"{cachedPersona}\nThis is your summary of recent conversations: {updateMessage}";

		// Pre-populate cache with existing persona
		_memoryCache.Set(Jiro.Core.Constants.CacheKeys.ComputedPersonaMessageKey, cachedPersona);

		// Act
		await _personaService.AddSummaryAsync(updateMessage!);

		// Assert - Check that the cache was updated even with empty/null message
		Assert.True(_memoryCache.TryGetValue(Jiro.Core.Constants.CacheKeys.ComputedPersonaMessageKey, out var updatedValue));
		Assert.Equal(expectedUpdatedPersona, updatedValue);
	}
}
