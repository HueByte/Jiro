using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using Jiro.Core.Services.System.Models;

using Microsoft.Extensions.Logging;

namespace Jiro.Core.Services.System;

/// <inheritdoc/>
public class LogsProviderService : ILogsProviderService
{
	private readonly ILogger<LogsProviderService> _logger;

	/// <summary>
	/// Initializes a new instance of the <see cref="LogsProviderService"/> class.
	/// </summary>
	/// <param name="logger">Logger instance for logging.</param>
	public LogsProviderService(ILogger<LogsProviderService> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	/// <inheritdoc/>
	public async Task<LogsResponse> GetLogsAsync(string? level = null, int limit = 100, int offset = 0,
		DateTime? fromDate = null, DateTime? toDate = null, string? searchTerm = null)
	{
		var logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
		var logPattern = "*.txt"; // Using .txt extension as seen in Logging folder

		try
		{
			_logger.LogInformation("Getting logs with level: {Level}, limit: {Limit}, offset: {Offset}, searchTerm: {SearchTerm}",
				level ?? "all", limit, offset, searchTerm ?? "none");

			// For better performance with large log files, we'll process files in reverse order
			// and stop when we have enough results
			var results = new List<LogEntry>();
			var totalProcessed = 0;
			var skipped = 0;

			if (Directory.Exists(logsDirectory))
			{
				var logFiles = Directory.GetFiles(logsDirectory, logPattern)
					.OrderByDescending(f => File.GetLastWriteTime(f));

				foreach (var logFile in logFiles)
				{
					try
					{
						// For large files, we might want to read in chunks or use streaming
						var lines = await File.ReadAllLinesAsync(logFile);

						// Process lines in reverse order (newest first) for better performance
						for (int i = lines.Length - 1; i >= 0; i--)
						{
							var line = lines[i];
							if (string.IsNullOrWhiteSpace(line)) continue;

							var logEntry = new LogEntry
							{
								File = Path.GetFileName(logFile),
								Timestamp = ExtractTimestamp(line),
								Level = ExtractLogLevel(line),
								Message = line
							};

							// Parse timestamp for accurate filtering
							var parsedTimestamp = TryParseTimestamp(logEntry.Timestamp);

							// Apply filters
							if (!PassesFilters(logEntry, parsedTimestamp, level, fromDate, toDate, searchTerm))
								continue;

							totalProcessed++;

							// Skip entries if we're not at the requested offset yet
							if (skipped < offset)
							{
								skipped++;
								continue;
							}

							// Add to results if we haven't reached the limit
							if (results.Count < limit)
							{
								results.Add(logEntry);
							}
							else if (totalProcessed > offset + limit)
							{
								// We have enough results and know there are more
								break;
							}
						}

						// If we have enough results and processed more than needed, we can stop
						if (results.Count >= limit && totalProcessed > offset + limit)
							break;
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Failed to read log file: {LogFile}", logFile);
					}
				}
			}

			// Get total count for pagination info (only if needed)
			var totalCount = totalProcessed;
			if (offset == 0 && results.Count < limit)
			{
				// If this is the first page and we have fewer results than the limit,
				// the total count is just the number of results
				totalCount = results.Count;
			}
			else if (totalProcessed <= offset + limit)
			{
				// If we processed all available logs, use that count
				totalCount = totalProcessed;
			}
			else
			{
				// Otherwise, we need to get the actual total count
				totalCount = await GetLogCountAsync(level, fromDate, toDate, searchTerm);
			}

			// Create response with backward compatibility
			var response = new LogsResponse
			{
				TotalLogs = totalCount,
				Level = level ?? "all",
				Limit = limit,
				Logs = results,
				RequestId = string.Empty, // Will be set by caller
			};

			// Try to set pagination properties if they exist (for newer versions of Jiro.Shared)
			try
			{
				var responseType = response.GetType();
				var hasMoreProperty = responseType.GetProperty("HasMore");
				var offsetProperty = responseType.GetProperty("Offset");

				if (hasMoreProperty != null && hasMoreProperty.CanWrite)
					hasMoreProperty.SetValue(response, totalCount > offset + results.Count);

				if (offsetProperty != null && offsetProperty.CanWrite)
					offsetProperty.SetValue(response, offset);
			}
			catch (Exception)
			{
				// Ignore reflection errors for backward compatibility
			}

			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving logs");
			throw;
		}
	}

	/// <inheritdoc/>
	public async Task<int> GetLogCountAsync(string? level = null, DateTime? fromDate = null,
		DateTime? toDate = null, string? searchTerm = null)
	{
		var logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
		var logPattern = "*.txt";
		var count = 0;

		try
		{
			if (Directory.Exists(logsDirectory))
			{
				var logFiles = Directory.GetFiles(logsDirectory, logPattern);

				foreach (var logFile in logFiles)
				{
					try
					{
						var lines = await File.ReadAllLinesAsync(logFile);

						foreach (var line in lines)
						{
							if (string.IsNullOrWhiteSpace(line)) continue;

							var logEntry = new LogEntry
							{
								Timestamp = ExtractTimestamp(line),
								Level = ExtractLogLevel(line),
								Message = line
							};

							var parsedTimestamp = TryParseTimestamp(logEntry.Timestamp);

							if (PassesFilters(logEntry, parsedTimestamp, level, fromDate, toDate, searchTerm))
								count++;
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Failed to read log file: {LogFile}", logFile);
					}
				}
			}

			return count;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error counting logs");
			return 0;
		}
	}

	/// <inheritdoc/>
	public async Task<IEnumerable<LogFileInfo>> GetLogFilesAsync()
	{
		var logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
		var logPattern = "*.txt";
		var files = new List<LogFileInfo>();

		try
		{
			if (Directory.Exists(logsDirectory))
			{
				var logFiles = Directory.GetFiles(logsDirectory, logPattern);

				foreach (var logFile in logFiles)
				{
					try
					{
						var fileInfo = new FileInfo(logFile);
						files.Add(new LogFileInfo
						{
							FileName = fileInfo.Name,
							FilePath = fileInfo.FullName,
							SizeBytes = fileInfo.Length,
							LastModified = fileInfo.LastWriteTime,
							Created = fileInfo.CreationTime
						});
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Failed to get info for log file: {LogFile}", logFile);
					}
				}
			}

			return files.OrderByDescending(f => f.LastModified);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving log files");
			return Enumerable.Empty<LogFileInfo>();
		}
	}

	/// <inheritdoc/>
	public async IAsyncEnumerable<LogEntry> StreamLogsAsync(string? level = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		// Future implementation for real-time log streaming
		// This would watch log files for changes and yield new entries
		var logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
		var logPattern = "*.txt";

		if (!Directory.Exists(logsDirectory))
			yield break;

		// For now, just yield existing logs (placeholder for future streaming implementation)
		var logFiles = Directory.GetFiles(logsDirectory, logPattern)
			.OrderByDescending(f => File.GetLastWriteTime(f))
			.Take(1); // Only latest file for streaming

		foreach (var logFile in logFiles)
		{
			if (cancellationToken.IsCancellationRequested)
				yield break;

			string[] lines;
			try
			{
				lines = await File.ReadAllLinesAsync(logFile, cancellationToken);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to read log file for streaming: {LogFile}", logFile);
				continue;
			}

			foreach (var line in lines.TakeLast(100)) // Last 100 entries for initial stream
			{
				if (cancellationToken.IsCancellationRequested)
					yield break;

				if (string.IsNullOrWhiteSpace(line)) continue;

				var logEntry = new LogEntry
				{
					File = Path.GetFileName(logFile),
					Timestamp = ExtractTimestamp(line),
					Level = ExtractLogLevel(line),
					Message = line
				};

				// Filter by level if specified - skip filtering if level is "all"
				if (!string.IsNullOrEmpty(level) && 
					!level.Equals("all", StringComparison.OrdinalIgnoreCase) &&
					!logEntry.Level.Equals(level, StringComparison.OrdinalIgnoreCase))
					continue;

				yield return logEntry;
			}
		}
	}

	private static string ExtractTimestamp(string logLine)
	{
		// Enhanced regex to match multiple timestamp formats
		var patterns = new[]
		{
			@"(\d{4}-\d{2}-\d{2}[\sT]\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?(?:Z|[+-]\d{2}:?\d{2})?)", // ISO format
			@"(\d{2}/\d{2}/\d{4} \d{2}:\d{2}:\d{2})", // US format
			@"(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})", // Standard format
		};

		foreach (var pattern in patterns)
		{
			var match = Regex.Match(logLine, pattern);
			if (match.Success)
				return match.Value;
		}

		return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
	}

	private static string ExtractLogLevel(string logLine)
	{
		var levels = new[] { "TRACE", "DEBUG", "INFO", "INFORMATION", "WARN", "WARNING", "ERROR", "FATAL", "CRITICAL" };
		foreach (var level in levels)
		{
			// Check for both [LEVEL] and |LEVEL| formats
			if (logLine.Contains($"[{level}]", StringComparison.OrdinalIgnoreCase) ||
				logLine.Contains($"|{level}|", StringComparison.OrdinalIgnoreCase))
			{
				return level.ToUpperInvariant();
			}
		}

		return "INFO";
	}

	private static DateTime TryParseTimestamp(string timestamp)
	{
		// Try various timestamp formats
		var formats = new[]
		{
			"yyyy-MM-dd HH:mm:ss",
			"yyyy-MM-ddTHH:mm:ss",
			"yyyy-MM-dd HH:mm:ss.fff",
			"yyyy-MM-ddTHH:mm:ss.fff",
			"yyyy-MM-ddTHH:mm:ss.fffffffZ",
			"yyyy-MM-ddTHH:mm:ss.fffZ",
			"MM/dd/yyyy HH:mm:ss",
			"dd/MM/yyyy HH:mm:ss"
		};

		foreach (var format in formats)
		{
			if (DateTime.TryParseExact(timestamp, format, CultureInfo.InvariantCulture,
				DateTimeStyles.None, out var result))
			{
				return result;
			}
		}

		// Fallback to general parsing
		if (DateTime.TryParse(timestamp, out var fallbackResult))
			return fallbackResult;

		return DateTime.UtcNow;
	}

	private static bool PassesFilters(LogEntry logEntry, DateTime parsedTimestamp,
		string? level, DateTime? fromDate, DateTime? toDate, string? searchTerm)
	{
		// Level filter - skip filtering if level is null, empty, or "all"
		if (!string.IsNullOrEmpty(level) && 
			!level.Equals("all", StringComparison.OrdinalIgnoreCase) &&
			!logEntry.Level.Equals(level, StringComparison.OrdinalIgnoreCase))
			return false;

		// Date range filter
		if (fromDate.HasValue && parsedTimestamp < fromDate.Value)
			return false;

		if (toDate.HasValue && parsedTimestamp > toDate.Value)
			return false;

		// Search term filter
		if (!string.IsNullOrEmpty(searchTerm) &&
			!logEntry.Message.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
			return false;

		return true;
	}
}

/// <summary>
/// Information about a log file
/// </summary>
public class LogFileInfo
{
	public string FileName { get; set; } = string.Empty;
	public string FilePath { get; set; } = string.Empty;
	public long SizeBytes { get; set; }
	public DateTime LastModified { get; set; }
	public DateTime Created { get; set; }
}
