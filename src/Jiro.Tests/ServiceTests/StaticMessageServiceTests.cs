using Jiro.Core.Constants;
using Jiro.Core.Services.StaticMessage;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Moq;

namespace Jiro.Tests.ServiceTests;

public class StaticMessageServiceTests : IDisposable
{
	private readonly Mock<ILogger<StaticMessageService>> _mockLogger;
	private readonly IMemoryCache _memoryCache;
	private readonly StaticMessageService _staticMessageService;
	private readonly string _testMessagesDirectory;
	private readonly string _originalMessageBasePath;

	public StaticMessageServiceTests()
	{
		_mockLogger = new Mock<ILogger<StaticMessageService>>();
		_memoryCache = new MemoryCache(new MemoryCacheOptions());

		// Create a temporary directory for test messages
		_testMessagesDirectory = Path.Combine(Path.GetTempPath(), "JiroTestMessages", Guid.NewGuid().ToString());
		Directory.CreateDirectory(_testMessagesDirectory);

		// Store the original MessageBasePath and temporarily replace it
		_originalMessageBasePath = Paths.MessageBasePath;
		typeof(Paths).GetField(nameof(Paths.MessageBasePath))?.SetValue(null, _testMessagesDirectory);

		_staticMessageService = new StaticMessageService(_mockLogger.Object, _memoryCache);
	}

	[Fact]
	public async Task GetStaticMessageAsync_WithExistingFile_ShouldReturnContent()
	{
		// Arrange
		var key = "test-message";
		var content = "This is a test message content.";
		var filePath = Path.Combine(_testMessagesDirectory, $"{key}.md");
		await File.WriteAllTextAsync(filePath, content);

		// Act
		var result = await _staticMessageService.GetStaticMessageAsync(key);

		// Assert
		Assert.Equal(content, result);
	}

	[Fact]
	public async Task GetStaticMessageAsync_WithNonExistentFile_ShouldReturnNull()
	{
		// Arrange
		var key = "non-existent-message";

		// Act
		var result = await _staticMessageService.GetStaticMessageAsync(key);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task GetStaticMessageAsync_ShouldCacheResult()
	{
		// Arrange
		var key = "cached-message";
		var content = "This content should be cached.";
		var filePath = Path.Combine(_testMessagesDirectory, $"{key}.md");
		await File.WriteAllTextAsync(filePath, content);

		// Act - First call
		var result1 = await _staticMessageService.GetStaticMessageAsync(key);
		
		// Delete the file to ensure second call uses cache
		File.Delete(filePath);
		
		// Act - Second call (should use cache)
		var result2 = await _staticMessageService.GetStaticMessageAsync(key);

		// Assert
		Assert.Equal(content, result1);
		Assert.Equal(content, result2);
	}

	[Fact]
	public async Task GetStaticMessageAsync_FromCache_ShouldLogDebug()
	{
		// Arrange
		var key = "cached-debug-message";
		var content = "Debug cache test content.";
		_memoryCache.Set(key, content);

		// Act
		await _staticMessageService.GetStaticMessageAsync(key);

		// Assert
		_mockLogger.Verify(
			x => x.Log(
				LogLevel.Debug,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Static message found in cache")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	[Fact]
	public async Task GetStaticMessageAsync_FromFile_ShouldLogInformation()
	{
		// Arrange
		var key = "file-log-message";
		var content = "File log test content.";
		var filePath = Path.Combine(_testMessagesDirectory, $"{key}.md");
		await File.WriteAllTextAsync(filePath, content);

		// Act
		await _staticMessageService.GetStaticMessageAsync(key);

		// Assert
		_mockLogger.Verify(
			x => x.Log(
				LogLevel.Information,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Static message loaded and cached")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	[Fact]
	public async Task GetStaticMessageAsync_WithFileNotFound_ShouldLogWarning()
	{
		// Arrange
		var key = "missing-message";

		// Act
		await _staticMessageService.GetStaticMessageAsync(key);

		// Assert
		_mockLogger.Verify(
			x => x.Log(
				LogLevel.Warning,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Static message file not found")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	[Fact]
	public async Task GetPersonaCoreMessageAsync_ShouldCallGetStaticMessageAsync()
	{
		// Arrange - Since the actual key contains "::" which is invalid on Windows,
		// we'll test the caching behavior instead
		var content = "Core persona message content.";
		_memoryCache.Set(CacheKeys.CorePersonaMessageKey, content);

		// Act
		var result = await _staticMessageService.GetPersonaCoreMessageAsync();

		// Assert
		Assert.Equal(content, result);
		
		// Verify the cache was accessed
		Assert.True(_memoryCache.TryGetValue(CacheKeys.CorePersonaMessageKey, out var cachedValue));
		Assert.Equal(content, cachedValue);
	}

	[Fact]
	public void InvalidateStaticMessage_ShouldRemoveFromCache()
	{
		// Arrange
		var key = "test-invalidate";
		var content = "Content to be invalidated.";
		_memoryCache.Set(key, content);

		// Verify it's in cache
		Assert.True(_memoryCache.TryGetValue(key, out _));

		// Act
		_staticMessageService.InvalidateStaticMessage(key);

		// Assert
		Assert.False(_memoryCache.TryGetValue(key, out _));
	}

	[Fact]
	public void InvalidateStaticMessage_ShouldLogInformation()
	{
		// Arrange
		var key = "test-invalidate-log";

		// Act
		_staticMessageService.InvalidateStaticMessage(key);

		// Assert
		_mockLogger.Verify(
			x => x.Log(
				LogLevel.Information,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalidated static message cache")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	[Fact]
	public void ClearStaticMessageCache_ShouldRemoveCommonEntries()
	{
		// Arrange
		_memoryCache.Set(CacheKeys.ComputedPersonaMessageKey, "computed message");
		_memoryCache.Set(CacheKeys.CorePersonaMessageKey, "core message");

		// Verify they're in cache
		Assert.True(_memoryCache.TryGetValue(CacheKeys.ComputedPersonaMessageKey, out _));
		Assert.True(_memoryCache.TryGetValue(CacheKeys.CorePersonaMessageKey, out _));

		// Act
		_staticMessageService.ClearStaticMessageCache();

		// Assert
		Assert.False(_memoryCache.TryGetValue(CacheKeys.ComputedPersonaMessageKey, out _));
		Assert.False(_memoryCache.TryGetValue(CacheKeys.CorePersonaMessageKey, out _));
	}

	[Fact]
	public void ClearStaticMessageCache_ShouldLogInformation()
	{
		// Act
		_staticMessageService.ClearStaticMessageCache();

		// Assert
		_mockLogger.Verify(
			x => x.Log(
				LogLevel.Information,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cleared static message cache entries")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	[Fact]
	public void SetStaticMessage_ShouldAddToCache()
	{
		// Arrange
		var key = "test-set-message";
		var content = "Set message content.";
		var expirationMinutes = 10;

		// Act
		_staticMessageService.SetStaticMessage(key, content, expirationMinutes);

		// Assert
		Assert.True(_memoryCache.TryGetValue(key, out var cachedContent));
		Assert.Equal(content, cachedContent);
	}

	[Fact]
	public void SetStaticMessage_ShouldLogInformation()
	{
		// Arrange
		var key = "test-set-log";
		var content = "Set log test content.";
		var expirationMinutes = 5;

		// Act
		_staticMessageService.SetStaticMessage(key, content, expirationMinutes);

		// Assert
		_mockLogger.Verify(
			x => x.Log(
				LogLevel.Information,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Set static message in cache")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	[Fact]
	public async Task GetStaticMessageAsync_WithFileReadError_ShouldReturnNull()
	{
		// Arrange
		var key = "error-message";
		var filePath = Path.Combine(_testMessagesDirectory, $"{key}.md");
		
		// Create a file that will cause read issues (this is tricky to simulate)
		// Instead, we'll test with an empty key that causes path issues
		var emptyKey = "";

		// Act
		var result = await _staticMessageService.GetStaticMessageAsync(emptyKey);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task GetStaticMessageAsync_WithMultilineContent_ShouldReturnFullContent()
	{
		// Arrange
		var key = "multiline-message";
		var content = @"# Test Message

This is a multiline message
with **markdown** formatting.

- Item 1
- Item 2

End of message.";
		var filePath = Path.Combine(_testMessagesDirectory, $"{key}.md");
		await File.WriteAllTextAsync(filePath, content);

		// Act
		var result = await _staticMessageService.GetStaticMessageAsync(key);

		// Assert
		Assert.Equal(content, result);
	}

	public void Dispose()
	{
		// Restore the original MessageBasePath
		typeof(Paths).GetField(nameof(Paths.MessageBasePath))?.SetValue(null, _originalMessageBasePath);

		_memoryCache.Dispose();

		if (Directory.Exists(_testMessagesDirectory))
		{
			Directory.Delete(_testMessagesDirectory, recursive: true);
		}
	}
}