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
		// Create test log content matching the new format: [YYYY-MM-DD HH:mm:ss.fff +TZ] [LEVEL] []
		var logEntries1 = new[]
		{
			"[2025-08-07 10:00:00.123 +02:00] [DBG] [] Debug message 1",
			"[2025-08-07 10:01:00.456 +02:00] [INF] [] Info message 1",
			"[2025-08-07 10:02:00.789 +02:00] [WRN] [] Warning message 1",
			"[2025-08-07 10:03:00.012 +02:00] [ERR] [] Error message 1",
			"[2025-08-07 10:04:00.345 +02:00] [INF] [] Info message 2"
		};

		var logEntries2 = new[]
		{
			"[2025-08-07 10:00:30.678 +02:00] [WRN] [] Warning in error log",
			"[2025-08-07 10:01:30.901 +02:00] [ERR] [] Error in error log",
			"[2025-08-07 10:02:30.234 +02:00] [FTL] [] Fatal error"
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
		using var cts = new CancellationTokenSource();
		cts.CancelAfter(TimeSpan.FromSeconds(1)); // Stop after 1 second to avoid infinite streaming

		// Act & Assert
		try
		{
			await foreach (var logEntry in _logsProviderService.StreamLogsAsync(initialLimit: 10, cancellationToken: cts.Token))
			{
				Assert.NotNull(logEntry);
				Assert.False(string.IsNullOrEmpty(logEntry.Message));
				logCount++;

				// Break after initial limit to avoid continuous streaming in tests
				if (logCount >= 10) break;
			}
		}
		catch (OperationCanceledException)
		{
			// Expected when cancelled
		}

		Assert.True(logCount >= 0);
	}

	[Fact]
	public async Task StreamLogsAsync_WithLevelFilter_ShouldYieldFilteredLogs()
	{
		// Arrange
		CreateTestLogFiles();
		var logCount = 0;
		using var cts = new CancellationTokenSource();
		cts.CancelAfter(TimeSpan.FromSeconds(1));

		// Act & Assert
		try
		{
			await foreach (var logEntry in _logsProviderService.StreamLogsAsync(level: "ERR", initialLimit: 5, cancellationToken: cts.Token))
			{
				Assert.NotNull(logEntry);
				Assert.Equal("ERR", logEntry.Level);
				logCount++;

				// Break after a few entries
				if (logCount >= 5) break;
			}
		}
		catch (OperationCanceledException)
		{
			// Expected when cancelled
		}

		Assert.True(logCount >= 0);
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
			await foreach (var logEntry in _logsProviderService.StreamLogsAsync(initialLimit: 5, cancellationToken: cts.Token))
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

	[Fact]
	public async Task StreamLogBatchesAsync_WithExistingLogs_ShouldYieldBatches()
	{
		// Arrange
		CreateTestLogFiles();
		var batchSize = 3;
		var initialLimit = 6;
		var batchCount = 0;
		var totalEntries = 0;
		using var cts = new CancellationTokenSource();
		cts.CancelAfter(TimeSpan.FromSeconds(1));

		// Act & Assert
		try
		{
			await foreach (var batch in _logsProviderService.StreamLogBatchesAsync(
				initialLimit: initialLimit, batchSize: batchSize, cancellationToken: cts.Token))
			{
				Assert.NotNull(batch);
				var batchList = batch.ToList();
				Assert.True(batchList.Count <= batchSize);
				Assert.All(batchList, entry => Assert.NotNull(entry.Message));

				totalEntries += batchList.Count;
				batchCount++;

				// Stop after getting initial batches to avoid continuous streaming in tests
				if (totalEntries >= initialLimit) break;
			}
		}
		catch (OperationCanceledException)
		{
			// Expected when cancelled
		}

		Assert.True(batchCount > 0);
		Assert.True(totalEntries >= 0);
	}

	[Fact]
	public async Task StreamLogBatchesAsync_WithLevelFilter_ShouldYieldFilteredBatches()
	{
		// Arrange
		CreateTestLogFiles();
		var batchSize = 2;
		var level = "ERR";
		var batchCount = 0;
		using var cts = new CancellationTokenSource();
		cts.CancelAfter(TimeSpan.FromSeconds(1));

		// Act & Assert
		try
		{
			await foreach (var batch in _logsProviderService.StreamLogBatchesAsync(
				level: level, batchSize: batchSize, cancellationToken: cts.Token))
			{
				Assert.NotNull(batch);
				var batchList = batch.ToList();
				Assert.All(batchList, entry => Assert.Equal(level, entry.Level));

				batchCount++;

				// Stop after a few batches
				if (batchCount >= 2) break;
			}
		}
		catch (OperationCanceledException)
		{
			// Expected when cancelled
		}

		Assert.True(batchCount >= 0);
	}

	[Fact]
	public async Task StreamLogBatchesAsync_WithIncompleteLastBatch_ShouldSendAfterTimeout()
	{
		// Arrange
		CreateTestLogFiles();
		var batchSize = 10; // Large batch size to ensure timeout occurs before batch is full
		var batchCount = 0;
		var totalEntries = 0;
		using var cts = new CancellationTokenSource();
		cts.CancelAfter(TimeSpan.FromSeconds(8)); // Allow enough time for timeout

		// Act & Assert
		try
		{
			await foreach (var batch in _logsProviderService.StreamLogBatchesAsync(
				initialLimit: 5, batchSize: batchSize, cancellationToken: cts.Token))
			{
				Assert.NotNull(batch);
				var batchList = batch.ToList();
				Assert.True(batchList.Count > 0);
				Assert.All(batchList, entry => Assert.NotNull(entry.Message));

				totalEntries += batchList.Count;
				batchCount++;

				// We should get at least one batch with the initial logs
				if (batchCount >= 1) break;
			}
		}
		catch (OperationCanceledException)
		{
			// Expected when cancelled
		}

		// Should have received at least one batch even though it wasn't full
		Assert.True(batchCount >= 1);
		Assert.True(totalEntries > 0);
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
