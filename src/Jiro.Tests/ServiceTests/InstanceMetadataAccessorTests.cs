using System.Net;
using System.Text;

using Jiro.Core.Constants;
using Jiro.Core.Options;
using Jiro.Core.Services.Context;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;
using Moq.Protected;

namespace Jiro.Tests.ServiceTests;

/// <summary>
/// Unit tests for the InstanceMetadataAccessor service.
/// </summary>
public class InstanceMetadataAccessorTests : IDisposable
{
	private readonly Mock<ILogger<InstanceMetadataAccessor>> _loggerMock;
	private readonly IMemoryCache _memoryCache;
	private readonly Mock<IInstanceContext> _instanceContextMock;
	private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
	private readonly Mock<IOptions<JiroCloudOptions>> _jiroCloudOptionsMock;
	private readonly JiroCloudOptions _jiroCloudOptions;
	private readonly InstanceMetadataAccessor _instanceMetadataAccessor;

	public InstanceMetadataAccessorTests()
	{
		_loggerMock = new Mock<ILogger<InstanceMetadataAccessor>>();
		_memoryCache = new MemoryCache(new MemoryCacheOptions());
		_instanceContextMock = new Mock<IInstanceContext>();
		_httpClientFactoryMock = new Mock<IHttpClientFactory>();
		_jiroCloudOptionsMock = new Mock<IOptions<JiroCloudOptions>>();

		_jiroCloudOptions = new JiroCloudOptions
		{
			ApiKey = "test-api-key",
			ApiUrl = "https://api.test.com"
		};

		_jiroCloudOptionsMock.Setup(x => x.Value).Returns(_jiroCloudOptions);

		_instanceMetadataAccessor = new InstanceMetadataAccessor(
			_loggerMock.Object,
			_memoryCache,
			_instanceContextMock.Object,
			_httpClientFactoryMock.Object,
			_jiroCloudOptionsMock.Object
		);
	}

	public void Dispose()
	{
		_memoryCache?.Dispose();
	}

	[Fact]
	public void Constructor_WithValidParameters_ShouldCreateInstance()
	{
		// Arrange & Act
		var accessor = new InstanceMetadataAccessor(
			_loggerMock.Object,
			_memoryCache,
			_instanceContextMock.Object,
			_httpClientFactoryMock.Object,
			_jiroCloudOptionsMock.Object
		);

		// Assert
		Assert.NotNull(accessor);
	}

	[Fact]
	public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
	{
		// Arrange & Act & Assert
		Assert.Throws<ArgumentNullException>(() => new InstanceMetadataAccessor(
			null!,
			_memoryCache,
			_instanceContextMock.Object,
			_httpClientFactoryMock.Object,
			_jiroCloudOptionsMock.Object
		));
	}

	[Fact]
	public void Constructor_WithNullMemoryCache_ShouldThrowArgumentNullException()
	{
		// Arrange & Act & Assert
		Assert.Throws<ArgumentNullException>(() => new InstanceMetadataAccessor(
			_loggerMock.Object,
			null!,
			_instanceContextMock.Object,
			_httpClientFactoryMock.Object,
			_jiroCloudOptionsMock.Object
		));
	}

	[Fact]
	public void Constructor_WithNullInstanceContext_ShouldThrowArgumentNullException()
	{
		// Arrange & Act & Assert
		Assert.Throws<ArgumentNullException>(() => new InstanceMetadataAccessor(
			_loggerMock.Object,
			_memoryCache,
			null!,
			_httpClientFactoryMock.Object,
			_jiroCloudOptionsMock.Object
		));
	}

	[Fact]
	public void Constructor_WithNullHttpClientFactory_ShouldThrowArgumentNullException()
	{
		// Arrange & Act & Assert
		Assert.Throws<ArgumentNullException>(() => new InstanceMetadataAccessor(
			_loggerMock.Object,
			_memoryCache,
			_instanceContextMock.Object,
			null!,
			_jiroCloudOptionsMock.Object
		));
	}

	[Fact]
	public void Constructor_WithNullApplicationOptions_ShouldThrowArgumentNullException()
	{
		// Arrange & Act & Assert
		Assert.Throws<ArgumentNullException>(() => new InstanceMetadataAccessor(
			_loggerMock.Object,
			_memoryCache,
			_instanceContextMock.Object,
			_httpClientFactoryMock.Object,
			null!
		));
	}

	[Fact]
	public async Task GetInstanceIdAsync_WithCachedValue_ShouldReturnCachedValue()
	{
		// Arrange
		const string expectedInstanceId = "cached-instance-id";
		_memoryCache.Set("StartupInstanceId", expectedInstanceId);

		// Act
		var result = await _instanceMetadataAccessor.GetInstanceIdAsync("");

		// Assert
		Assert.Equal(expectedInstanceId, result);
		_httpClientFactoryMock.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public async Task GetInstanceIdAsync_WithoutCachedValue_ShouldFetchFromApi()
	{
		// Arrange
		const string expectedInstanceId = "api-instance-id";
		const string apiResponse = """{"data": {"id": "api-instance-id", "name": "Test Instance", "description": "Test Description", "createdAt": "2024-01-01T00:00:00Z", "lastOnline": "2024-01-01T00:00:00Z", "apiKey": "test-api-key", "userId": "test-user"}}""";

		var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
		var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(apiResponse, Encoding.UTF8, "application/json")
		};

		httpMessageHandlerMock.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(httpResponseMessage);

		var httpClient = new HttpClient(httpMessageHandlerMock.Object)
		{
			BaseAddress = new Uri(_jiroCloudOptions.ApiUrl)
		};

		_httpClientFactoryMock.Setup(x => x.CreateClient(HttpClients.JIRO)).Returns(httpClient);

		// Act
		var result = await _instanceMetadataAccessor.GetInstanceIdAsync("");

		// Assert
		Assert.Equal(expectedInstanceId, result);
		Assert.True(_memoryCache.TryGetValue("StartupInstanceId", out var cachedValue));
		Assert.Equal(expectedInstanceId, cachedValue);
	}

	[Fact]
	public async Task GetInstanceIdAsync_WithApiError_ShouldReturnNull()
	{
		// Arrange
		var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
		var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);

		httpMessageHandlerMock.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(httpResponseMessage);

		var httpClient = new HttpClient(httpMessageHandlerMock.Object)
		{
			BaseAddress = new Uri(_jiroCloudOptions.ApiUrl)
		};

		_httpClientFactoryMock.Setup(x => x.CreateClient(HttpClients.JIRO)).Returns(httpClient);

		// Act
		var result = await _instanceMetadataAccessor.GetInstanceIdAsync("");

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public void GetCurrentInstanceId_WithCachedValue_ShouldReturnCachedValue()
	{
		// Arrange
		const string expectedInstanceId = "cached-instance-id";
		_memoryCache.Set("StartupInstanceId", expectedInstanceId);

		// Act
		var result = _instanceMetadataAccessor.GetCurrentInstanceId();

		// Assert
		Assert.Equal(expectedInstanceId, result);
	}

	[Fact]
	public void GetCurrentInstanceId_WithoutCachedValue_ShouldFallbackToInstanceContext()
	{
		// Arrange
		const string expectedInstanceId = "context-instance-id";
		_instanceContextMock.Setup(x => x.InstanceId).Returns(expectedInstanceId);

		// Act
		var result = _instanceMetadataAccessor.GetCurrentInstanceId();

		// Assert
		Assert.Equal(expectedInstanceId, result);
	}

	[Fact]
	public void GetCurrentInstanceId_WithoutCachedValueAndEmptyContext_ShouldReturnNull()
	{
		// Arrange
		_instanceContextMock.Setup(x => x.InstanceId).Returns((string?)null);

		// Act
		var result = _instanceMetadataAccessor.GetCurrentInstanceId();

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public void InvalidateInstanceCache_ShouldRemoveCachedValue()
	{
		// Arrange
		const string instanceId = "test-instance-id";
		_memoryCache.Set("StartupInstanceId", instanceId);

		// Act
		_instanceMetadataAccessor.InvalidateInstanceCache("");

		// Assert
		Assert.False(_memoryCache.TryGetValue("StartupInstanceId", out _));
	}

	[Fact]
	public void ClearInstanceCache_ShouldRemoveCachedValue()
	{
		// Arrange
		const string instanceId = "test-instance-id";
		_memoryCache.Set("StartupInstanceId", instanceId);

		// Act
		_instanceMetadataAccessor.ClearInstanceCache();

		// Assert
		Assert.False(_memoryCache.TryGetValue("StartupInstanceId", out _));
	}

	[Fact]
	public async Task FetchInstanceIdFromApiAsync_WithValidApiKey_ShouldReturnInstanceId()
	{
		// Arrange
		const string apiKey = "valid-api-key";
		const string expectedInstanceId = "api-instance-id";
		const string apiResponse = """{"data": {"id": "api-instance-id", "name": "Test Instance", "description": "Test Description", "createdAt": "2024-01-01T00:00:00Z", "lastOnline": "2024-01-01T00:00:00Z", "apiKey": "valid-api-key", "userId": "test-user"}}""";

		var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
		var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(apiResponse, Encoding.UTF8, "application/json")
		};

		httpMessageHandlerMock.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.Query.Contains($"apiKey={Uri.EscapeDataString(apiKey)}")),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(httpResponseMessage);

		var httpClient = new HttpClient(httpMessageHandlerMock.Object)
		{
			BaseAddress = new Uri(_jiroCloudOptions.ApiUrl)
		};

		_httpClientFactoryMock.Setup(x => x.CreateClient(HttpClients.JIRO)).Returns(httpClient);

		// Act
		var result = await _instanceMetadataAccessor.FetchInstanceIdFromApiAsync(apiKey);

		// Assert
		Assert.Equal(expectedInstanceId, result);
	}

	[Fact]
	public async Task FetchInstanceIdFromApiAsync_WithEmptyApiKey_ShouldReturnNull()
	{
		// Arrange & Act
		var result = await _instanceMetadataAccessor.FetchInstanceIdFromApiAsync("");

		// Assert
		Assert.Null(result);
		_httpClientFactoryMock.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public async Task FetchInstanceIdFromApiAsync_WithNullApiKey_ShouldReturnNull()
	{
		// Arrange & Act
		var result = await _instanceMetadataAccessor.FetchInstanceIdFromApiAsync(null!);

		// Assert
		Assert.Null(result);
		_httpClientFactoryMock.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public async Task FetchInstanceIdFromApiAsync_WithInvalidJson_ShouldReturnNull()
	{
		// Arrange
		const string apiKey = "valid-api-key";
		const string invalidJsonResponse = "invalid json";

		var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
		var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(invalidJsonResponse, Encoding.UTF8, "application/json")
		};

		httpMessageHandlerMock.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(httpResponseMessage);

		var httpClient = new HttpClient(httpMessageHandlerMock.Object)
		{
			BaseAddress = new Uri(_jiroCloudOptions.ApiUrl)
		};

		_httpClientFactoryMock.Setup(x => x.CreateClient(HttpClients.JIRO)).Returns(httpClient);

		// Act
		var result = await _instanceMetadataAccessor.FetchInstanceIdFromApiAsync(apiKey);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task InitializeInstanceIdAsync_WithValidApiKey_ShouldFetchAndCacheInstanceId()
	{
		// Arrange
		const string apiKey = "valid-api-key";
		const string expectedInstanceId = "initialized-instance-id";
		const string apiResponse = """{"data": {"id": "initialized-instance-id", "name": "Test Instance", "description": "Test Description", "createdAt": "2024-01-01T00:00:00Z", "lastOnline": "2024-01-01T00:00:00Z", "apiKey": "valid-api-key", "userId": "test-user"}}""";

		var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
		var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(apiResponse, Encoding.UTF8, "application/json")
		};

		httpMessageHandlerMock.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(httpResponseMessage);

		var httpClient = new HttpClient(httpMessageHandlerMock.Object)
		{
			BaseAddress = new Uri(_jiroCloudOptions.ApiUrl)
		};

		_httpClientFactoryMock.Setup(x => x.CreateClient(HttpClients.JIRO)).Returns(httpClient);

		// Act
		var result = await _instanceMetadataAccessor.InitializeInstanceIdAsync(apiKey);

		// Assert
		Assert.Equal(expectedInstanceId, result);
		Assert.True(_memoryCache.TryGetValue("StartupInstanceId", out var cachedValue));
		Assert.Equal(expectedInstanceId, cachedValue);
	}

	[Fact]
	public async Task InitializeInstanceIdAsync_WithEmptyApiKey_ShouldReturnNull()
	{
		// Arrange & Act
		var result = await _instanceMetadataAccessor.InitializeInstanceIdAsync("");

		// Assert
		Assert.Null(result);
		_httpClientFactoryMock.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Never);
	}

	[Theory]
	[InlineData("")]
	[InlineData("session-1")]
	[InlineData("session-2")]
	public async Task GetInstanceIdAsync_WithDifferentSessionIds_ShouldIgnoreSessionIdParameter(string sessionId)
	{
		// Arrange
		const string expectedInstanceId = "global-instance-id";
		_memoryCache.Set("StartupInstanceId", expectedInstanceId);

		// Act
		var result = await _instanceMetadataAccessor.GetInstanceIdAsync(sessionId);

		// Assert
		Assert.Equal(expectedInstanceId, result);
	}

	[Theory]
	[InlineData("")]
	[InlineData("session-1")]
	[InlineData("session-2")]
	public void InvalidateInstanceCache_WithDifferentSessionIds_ShouldClearGlobalCache(string sessionId)
	{
		// Arrange
		const string instanceId = "test-instance-id";
		_memoryCache.Set("StartupInstanceId", instanceId);

		// Act
		_instanceMetadataAccessor.InvalidateInstanceCache(sessionId);

		// Assert
		Assert.False(_memoryCache.TryGetValue("StartupInstanceId", out _));
	}
}
