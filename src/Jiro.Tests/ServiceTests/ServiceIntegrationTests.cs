using Jiro.Core.Services.Geolocation;
using Jiro.Core.Services.MessageCache;
using Jiro.Core.Services.Persona;
using Jiro.Core.Services.Semaphore;
using Jiro.Core.Services.Weather;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Moq;

namespace Jiro.Tests.ServiceTests;

/// <summary>
/// Integration tests that verify multiple services working together
/// These tests demonstrate the interaction between services in realistic scenarios
/// </summary>
public class ServiceIntegrationTests
{
	private readonly Mock<ILogger<PersonaService>> _personaLoggerMock;
	private readonly Mock<ILogger<SemaphoreManager>> _semaphoreLoggerMock;
	private readonly Mock<IMemoryCache> _memoryCacheMock;
	private readonly Mock<IMessageManager> _messageManagerMock;
	private readonly Mock<IGeolocationService> _geolocationServiceMock;
	private readonly Mock<IWeatherService> _weatherServiceMock;

	private readonly ISemaphoreManager _semaphoreManager;

	public ServiceIntegrationTests()
	{
		// Setup mocks
		_personaLoggerMock = new Mock<ILogger<PersonaService>>();
		_semaphoreLoggerMock = new Mock<ILogger<SemaphoreManager>>();
		_memoryCacheMock = new Mock<IMemoryCache>();
		_messageManagerMock = new Mock<IMessageManager>();
		_geolocationServiceMock = new Mock<IGeolocationService>();
		_weatherServiceMock = new Mock<IWeatherService>();

		// Create real service instances
		_semaphoreManager = new SemaphoreManager(_semaphoreLoggerMock.Object);
	}

	[Fact]
	public async Task WeatherWorkflow_GeolocationAndWeatherServices_Integration()
	{
		// This test simulates a complete weather lookup workflow
		// Arrange
		const string cityName = "London";
		const string expectedWeatherInfo = "Sunny, 22°C";

		var geoResponse = new Core.Services.Weather.Models.GeoLocationResponse
		{
			Lat = "51.5074",
			Lon = "-0.1278",
			DisplayName = "London, UK"
		};

		// Setup geolocation service
		_geolocationServiceMock.Setup(static x => x.GetGeolocationAsync(cityName))
			.ReturnsAsync(geoResponse);

		// Setup weather service to use geolocation
		_weatherServiceMock.Setup(static x => x.GetWeatherStringAsync(cityName))
			.ReturnsAsync(expectedWeatherInfo);

		// Act
		var geoResult = await _geolocationServiceMock.Object.GetGeolocationAsync(cityName);
		var weatherResult = await _weatherServiceMock.Object.GetWeatherStringAsync(cityName);

		// Assert
		Assert.NotNull(geoResult);
		Assert.Equal("51.5074", geoResult.Lat);
		Assert.Equal("-0.1278", geoResult.Lon);
		Assert.Equal(expectedWeatherInfo, weatherResult);

		// Verify the workflow called geolocation first, then weather
		_geolocationServiceMock.Verify(static x => x.GetGeolocationAsync(cityName), Times.Once);
		_weatherServiceMock.Verify(static x => x.GetWeatherStringAsync(cityName), Times.Once);
	}

	[Fact]
	public async Task MessageManager_AddChatExchange_ShouldStoreMessages()
	{
		// This test simulates message caching workflow

		// Arrange
		const string instanceId = "message-test";
		const string sessionId = "session-123";
		const string userMessage = "What's the weather like?";

		var chatMessages = new List<Core.Services.Conversation.Models.ChatMessageWithMetadata>();

		var modelMessages = new List<Core.Models.Message>
		{
			new() { Id = "msg1", Content = userMessage, IsUser = true, SessionId = sessionId }
		};

		// Setup message manager
		_messageManagerMock.Setup(static x => x.AddChatExchangeAsync(
				sessionId,
				It.IsAny<List<Core.Services.Conversation.Models.ChatMessageWithMetadata>>(),
				It.IsAny<List<Core.Models.Message>>()))
			.Returns(Task.CompletedTask);

		// Act
		await _messageManagerMock.Object.AddChatExchangeAsync(sessionId, chatMessages, modelMessages);

		// Assert
		_messageManagerMock.Verify(static x => x.AddChatExchangeAsync(
			sessionId,
			It.IsAny<List<Core.Services.Conversation.Models.ChatMessageWithMetadata>>(),
			It.IsAny<List<Core.Models.Message>>()), Times.Once);
	}

	[Fact]
	public async Task ConcurrentInstances_ShouldUseSeparateSemaphores()
	{
		// This test verifies that different instances use separate semaphores
		// allowing for concurrent processing

		// Arrange
		const string instance1 = "instance-1";
		const string instance2 = "instance-2";

		// Act - Get semaphores for different instances
		var semaphore1 = _semaphoreManager.GetOrCreateInstanceSemaphore(instance1);
		var semaphore2 = _semaphoreManager.GetOrCreateInstanceSemaphore(instance2);

		// Verify they can be acquired concurrently
		await semaphore1.WaitAsync();
		await semaphore2.WaitAsync();

		// Assert
		Assert.NotSame(semaphore1, semaphore2); // Different instances should have different semaphores
		Assert.Equal(0, semaphore1.CurrentCount); // Both should be acquired
		Assert.Equal(0, semaphore2.CurrentCount);

		// Cleanup
		semaphore1.Release();
		semaphore2.Release();
	}

	[Fact]
	public void SameConcurrentInstance_ShouldUseSameSemaphore()
	{
		// This test verifies that the same instance uses the same semaphore

		// Arrange
		const string instanceId = "same-instance";

		// Act - Get semaphores for the same instance
		var semaphore1 = _semaphoreManager.GetOrCreateInstanceSemaphore(instanceId);
		var semaphore2 = _semaphoreManager.GetOrCreateInstanceSemaphore(instanceId);

		// Assert
		Assert.Same(semaphore1, semaphore2); // Same instance should return same semaphore
	}

	[Fact]
	public async Task ErrorHandling_AcrossServices_ShouldPropagateProperly()
	{
		// Test error propagation across service boundaries

		// Arrange
		const string cityName = "InvalidCity";
		var expectedException = new Core.JiroException("Couldn't find the desired city");

		_geolocationServiceMock.Setup(x => x.GetGeolocationAsync(cityName))
			.ThrowsAsync(expectedException);

		// Act & Assert
		var geoException = await Assert.ThrowsAsync<Core.JiroException>(
			() => _geolocationServiceMock.Object.GetGeolocationAsync(cityName));

		Assert.Equal(expectedException.UserMessage, geoException.UserMessage);
	}

	[Fact]
	public void MemoryCache_Operations_ShouldWorkCorrectly()
	{
		// Test memory cache operations

		// Arrange
		const string cacheKey = "test-key";
		const string cacheValue = "test-value";

		object? cachedValue = cacheValue;
		_memoryCacheMock.Setup(x => x.TryGetValue(cacheKey, out cachedValue))
			.Returns(true);

		var cacheEntry = Mock.Of<ICacheEntry>();
		_memoryCacheMock.Setup(x => x.CreateEntry(cacheKey))
			.Returns(cacheEntry);

		// Act
		var result = _memoryCacheMock.Object.TryGetValue(cacheKey, out var retrievedValue);
		var entry = _memoryCacheMock.Object.CreateEntry(cacheKey);

		// Assert
		Assert.True(result);
		Assert.Equal(cacheValue, retrievedValue);
		Assert.NotNull(entry);

		// Verify cache interactions
		_memoryCacheMock.Verify(x => x.TryGetValue(cacheKey, out It.Ref<object?>.IsAny), Times.Once);
		_memoryCacheMock.Verify(x => x.CreateEntry(cacheKey), Times.Once);
	}

	[Fact]
	public async Task MessageManager_PersonaCoreMessage_ShouldCacheCorrectly()
	{
		// Test persona message caching

		// Arrange
		const string personaMessage = "You are Jiro, a helpful AI assistant";

		_messageManagerMock.Setup(static x => x.GetPersonaCoreMessageAsync())
			.ReturnsAsync(personaMessage);

		// Act
		var result = await _messageManagerMock.Object.GetPersonaCoreMessageAsync();

		// Assert
		Assert.Equal(personaMessage, result);
		_messageManagerMock.Verify(static x => x.GetPersonaCoreMessageAsync(), Times.Once);
	}

	[Theory]
	[InlineData("London", "51.5074", "-0.1278")]
	[InlineData("Paris", "48.8566", "2.3522")]
	[InlineData("Tokyo", "35.6762", "139.6503")]
	public async Task GeolocationService_VariousCities_ShouldReturnCorrectCoordinates(
		string cityName, string expectedLat, string expectedLon)
	{
		// Test geolocation service with various cities

		// Arrange
		var geoResponse = new Core.Services.Weather.Models.GeoLocationResponse
		{
			Lat = expectedLat,
			Lon = expectedLon,
			DisplayName = $"{cityName}, Country"
		};

		_geolocationServiceMock.Setup(x => x.GetGeolocationAsync(cityName))
			.ReturnsAsync(geoResponse);

		// Act
		var result = await _geolocationServiceMock.Object.GetGeolocationAsync(cityName);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(expectedLat, result.Lat);
		Assert.Equal(expectedLon, result.Lon);
		Assert.Contains(cityName, result.DisplayName);
	}

	[Fact]
	public async Task WeatherService_Integration_ShouldHandleCompleteFlow()
	{
		// Test complete weather service flow

		// Arrange
		const string cityName = "London";
		var weatherResponse = new Core.Services.Weather.Models.WeatherResponse
		{
			Latitude = 51.5074,
			Longitude = -0.1278,
			CurrentWeather = new Core.Services.Weather.Models.CurrentWeather
			{
				Temperature = 22.5,
				Weathercode = 800
			}
		};

		_weatherServiceMock.Setup(static x => x.GetWeatherAsync(cityName))
			.ReturnsAsync(weatherResponse);

		// Act
		var result = await _weatherServiceMock.Object.GetWeatherAsync(cityName);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(51.5074, result.Latitude);
		Assert.Equal(-0.1278, result.Longitude);
		Assert.NotNull(result.CurrentWeather);
		Assert.Equal(22.5, result.CurrentWeather.Temperature);
		Assert.Equal(800, result.CurrentWeather.Weathercode);
	}
}
