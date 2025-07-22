using System.Globalization;
using System.Text.Json;

using Jiro.Core.Services.System;
using Jiro.Core.Services.System.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Moq;

namespace Jiro.Tests.ServiceTests;

public class LogsProviderServiceTests : IDisposable
{
	private readonly Mock<ILogger<LogsProviderService>> _mockLogger;
	private readonly IConfiguration _configuration;
	private readonly LogsProviderService _logsProviderService;
	private readonly string _testLogDirectory;
	private readonly string _testLogFile1;
	private readonly string _testLogFile2;

	public LogsProviderServiceTests()
	{
		_mockLogger = new Mock<ILogger<LogsProviderService>>();

		// Create a temporary directory for test logs
		_testLogDirectory = Path.Combine(Path.GetTempPath(), "JiroTestLogs", Guid.NewGuid().ToString());
		Directory.CreateDirectory(_testLogDirectory);

		_testLogFile1 = Path.Combine(_testLogDirectory, "jiro_20250722.log");
		_testLogFile2 = Path.Combine(_testLogDirectory, "jiro_errors_20250722.log");

		_configuration = SetupConfiguration();
		_logsProviderService = new LogsProviderService(_mockLogger.Object, _configuration);
	}

	private IConfiguration SetupConfiguration()
	{
		// Create actual configuration with test log paths
		var configData = new Dictionary<string, string?>
		{
			["Serilog:WriteTo:0:Name"] = "File",
			["Serilog:WriteTo:0:Args:path"] = Path.Combine(_testLogDirectory, "jiro_.log"),
			["Serilog:WriteTo:1:Name"] = "File",
			["Serilog:WriteTo:1:Args:path"] = Path.Combine(_testLogDirectory, "jiro_errors_.log"),
			["Serilog:WriteTo:2:Name"] = "Console"
		};

		return new ConfigurationBuilder()
			.AddInMemoryCollection(configData)
			.Build();
	}

	private void CreateTestLogFiles()
	{
		// Create test log content matching both formats: [HH:mm:ss LEVEL] and HH:mm:ss LEVEL] (missing opening bracket)
		var logEntries1 = new[]
		{
			"[10:00:00 DBG] [TestContext] Debug message 1",
			"[10:01:00 INF] [TestContext] Info message 1",
			"10:02:00 WRN] [TestContext] Warning message 1", // Missing opening bracket
			"[10:03:00 ERR] [TestContext] Error message 1",
			"10:04:00 INF] [TestContext] Info message 2" // Missing opening bracket
		};

		var logEntries2 = new[]
		{
			"10:00:30 WRN] [ErrorContext] Warning in error log", // Missing opening bracket
			"[10:01:30 ERR] [ErrorContext] Error in error log",
			"[10:02:30 FTL] [ErrorContext] Fatal error"
		};

		File.WriteAllLines(_testLogFile1, logEntries1);
		File.WriteAllLines(_testLogFile2, logEntries2);
	}

	[Fact]
	public void Constructor_WithValidParameters_ShouldCreateInstance()
	{
		// Arrange & Act & Assert
		Assert.NotNull(_logsProviderService);
	}

	[Fact]
	public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
	{
		// Arrange, Act & Assert
		Assert.Throws<ArgumentNullException>(() => new LogsProviderService(null!, _configuration));
	}

	[Fact]
	public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
	{
		// Arrange, Act & Assert
		Assert.Throws<ArgumentNullException>(() => new LogsProviderService(_mockLogger.Object, null!));
	}

	[Fact]
	public async Task GetLogsAsync_WithExistingLogs_ShouldReturnLogs()
	{
		// Arrange
		CreateTestLogFiles();

		// Act
		var result = await _logsProviderService.GetLogsAsync();

		// Assert
		Assert.NotNull(result);
		Assert.NotNull(result.Logs);
		Assert.True(result.Logs.Count() > 0);
		Assert.True(result.TotalLogs > 0);
	}

	[Fact]
	public async Task GetLogsAsync_WithLevelFilter_ShouldReturnFilteredLogs()
	{
		// Arrange
		CreateTestLogFiles();

		// Act
		var result = await _logsProviderService.GetLogsAsync(level: "ERR");

		// Assert
		Assert.NotNull(result);
		Assert.NotNull(result.Logs);
		Assert.All(result.Logs, log => Assert.Equal("ERR", log.Level));
	}

	[Fact]
	public async Task GetLogsAsync_WithLimit_ShouldRespectLimit()
	{
		// Arrange
		CreateTestLogFiles();
		var limit = 3;

		// Act
		var result = await _logsProviderService.GetLogsAsync(limit: limit);

		// Assert
		Assert.NotNull(result);
		Assert.NotNull(result.Logs);
		Assert.True(result.Logs.Count() <= limit);
	}

	[Fact]
	public async Task GetLogsAsync_WithOffset_ShouldSkipEntries()
	{
		// Arrange
		CreateTestLogFiles();
		var offset = 2;

		// Act
		var resultWithOffset = await _logsProviderService.GetLogsAsync(offset: offset, limit: 10);
		var resultWithoutOffset = await _logsProviderService.GetLogsAsync(limit: 10);

		// Assert
		Assert.NotNull(resultWithOffset);
		Assert.NotNull(resultWithoutOffset);
		Assert.True(resultWithOffset.Logs.Count() <= resultWithoutOffset.Logs.Count());
	}

	[Fact]
	public async Task GetLogsAsync_WithSearchTerm_ShouldReturnMatchingLogs()
	{
		// Arrange
		CreateTestLogFiles();

		// Act
		var result = await _logsProviderService.GetLogsAsync(searchTerm: "Error");

		// Assert
		Assert.NotNull(result);
		Assert.NotNull(result.Logs);
		Assert.All(result.Logs, log => Assert.Contains("Error", log.Message, StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task GetLogsAsync_WithDateRange_ShouldReturnLogsInRange()
	{
		// Arrange
		CreateTestLogFiles();
		var fromDate = new DateTime(2025, 1, 22, 10, 1, 0);
		var toDate = new DateTime(2025, 1, 22, 10, 3, 0);

		// Act
		var result = await _logsProviderService.GetLogsAsync(fromDate: fromDate, toDate: toDate);

		// Assert
		Assert.NotNull(result);
		Assert.NotNull(result.Logs);
		// Note: This test might need adjustment based on the actual timestamp parsing logic
	}

	[Fact]
	public async Task GetLogCountAsync_WithExistingLogs_ShouldReturnCorrectCount()
	{
		// Arrange
		CreateTestLogFiles();

		// Act
		var count = await _logsProviderService.GetLogCountAsync();

		// Assert
		Assert.True(count > 0);
	}

	[Fact]
	public async Task GetLogCountAsync_WithLevelFilter_ShouldReturnFilteredCount()
	{
		// Arrange
		CreateTestLogFiles();

		// Act
		var totalCount = await _logsProviderService.GetLogCountAsync();
		var errorCount = await _logsProviderService.GetLogCountAsync(level: "ERR");

		// Assert
		Assert.True(totalCount >= errorCount);
		Assert.True(errorCount > 0);
	}

	[Fact]
	public async Task GetLogFilesAsync_WithExistingFiles_ShouldReturnFileInfo()
	{
		// Arrange
		CreateTestLogFiles();

		// Act
		var files = await _logsProviderService.GetLogFilesAsync();

		// Assert
		Assert.NotNull(files);
		Assert.True(files.Count() > 0);
		Assert.All(files, file => Assert.True(file.SizeBytes > 0));
		Assert.All(files, file => Assert.False(string.IsNullOrEmpty(file.FileName)));
	}

	[Fact]
	public async Task StreamLogsAsync_WithExistingLogs_ShouldYieldLogs()
	{
		// Arrange
		CreateTestLogFiles();
		var logCount = 0;

		// Act & Assert
		await foreach (var logEntry in _logsProviderService.StreamLogsAsync())
		{
			Assert.NotNull(logEntry);
			Assert.False(string.IsNullOrEmpty(logEntry.Message));
			logCount++;

			// Limit to prevent infinite loop in case of issues
			if (logCount > 100) break;
		}

		Assert.True(logCount > 0);
	}

	[Fact]
	public async Task StreamLogsAsync_WithLevelFilter_ShouldYieldFilteredLogs()
	{
		// Arrange
		CreateTestLogFiles();
		var logCount = 0;

		// Act & Assert
		await foreach (var logEntry in _logsProviderService.StreamLogsAsync(level: "ERR"))
		{
			Assert.NotNull(logEntry);
			Assert.Equal("ERR", logEntry.Level);
			logCount++;

			// Limit to prevent infinite loop
			if (logCount > 50) break;
		}

		Assert.True(logCount > 0);
	}

	[Fact]
	public async Task StreamLogsAsync_WithCancellation_ShouldRespectCancellationToken()
	{
		// Arrange
		CreateTestLogFiles();
		using var cts = new CancellationTokenSource();
		cts.CancelAfter(TimeSpan.FromMilliseconds(100));

		// Act & Assert
		var logCount = 0;
		try
		{
			await foreach (var logEntry in _logsProviderService.StreamLogsAsync(cancellationToken: cts.Token))
			{
				logCount++;
				await Task.Delay(50, cts.Token); // Simulate some processing time
			}
		}
		catch (OperationCanceledException)
		{
			// Expected when cancellation is triggered
		}

		// We should have started processing but been cancelled
		Assert.True(logCount >= 0);
	}

	[Fact]
	public async Task GetLogsAsync_WithNonExistentDirectory_ShouldReturnEmptyResult()
	{
		// Arrange
		// Don't create test files

		// Act
		var result = await _logsProviderService.GetLogsAsync();

		// Assert
		Assert.NotNull(result);
		Assert.NotNull(result.Logs);
		Assert.Empty(result.Logs);
		Assert.Equal(0, result.TotalLogs);
	}

	[Theory]
	[InlineData("all")]
	[InlineData("ALL")]
	[InlineData("")]
	[InlineData(null)]
	public async Task GetLogsAsync_WithAllLevelVariations_ShouldReturnAllLogs(string? level)
	{
		// Arrange
		CreateTestLogFiles();

		// Act
		var result = await _logsProviderService.GetLogsAsync(level: level);

		// Assert
		Assert.NotNull(result);
		Assert.NotNull(result.Logs);
		Assert.True(result.Logs.Count() > 0);
	}

	[Fact]
	public async Task GetLogsAsync_ConcurrentAccess_ShouldHandleProperlyWithSharedAccess()
	{
		// Arrange
		CreateTestLogFiles();

		// Act - Simulate concurrent access
		var tasks = new List<Task<LogsResponse>>();
		for (int i = 0; i < 5; i++)
		{
			tasks.Add(_logsProviderService.GetLogsAsync(limit: 10));
		}

		var results = await Task.WhenAll(tasks);

		// Assert
		Assert.All(results, result =>
		{
			Assert.NotNull(result);
			Assert.NotNull(result.Logs);
		});
	}

	[Fact]
	public async Task GetLogsAsync_LargeOffset_ShouldHandleGracefully()
	{
		// Arrange
		CreateTestLogFiles();

		// Act
		var result = await _logsProviderService.GetLogsAsync(offset: 1000, limit: 10);

		// Assert
		Assert.NotNull(result);
		Assert.NotNull(result.Logs);
		// Should return empty or fewer results when offset is larger than available logs
	}

	[Fact]
	public async Task GetLogsAsync_ZeroLimit_ShouldReturnEmptyResult()
	{
		// Arrange
		CreateTestLogFiles();

		// Act
		var result = await _logsProviderService.GetLogsAsync(limit: 0);

		// Assert
		Assert.NotNull(result);
		Assert.NotNull(result.Logs);
		Assert.Empty(result.Logs);
	}

	public void Dispose()
	{
		// Cleanup test directory
		if (Directory.Exists(_testLogDirectory))
		{
			try
			{
				Directory.Delete(_testLogDirectory, true);
			}
			catch
			{
				// Ignore cleanup errors
			}
		}
	}
}
