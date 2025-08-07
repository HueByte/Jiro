using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

using Jiro.Core.Services.System.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jiro.Core.Services.System;

/// <inheritdoc/>
public class LogsProviderService : ILogsProviderService
{
	private readonly ILogger<LogsProviderService> _logger;
	private readonly IConfiguration _configuration;

	private static readonly Lazy<Regex> DefaultTimestampRegex = new(() =>
		new Regex(@"^\[(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}(?:\.\d{3})?(?: [+-]\d{2}:\d{2})?)\]\s*\[(?<level>[A-Z]+)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled));

	private static readonly Lazy<Regex> DefaultLogLevelRegex = new(() =>
		new Regex(@"^\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}(?:\.\d{3})?(?: [+-]\d{2}:\d{2})?\]\s*\[(?<level>[A-Z]+)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled));

	// Regex to detect the start of a new log entry - handles new format with timezone
	private static readonly Lazy<Regex> LogEntryStartRegex = new(() =>
		new Regex(@"^\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}(?:\.\d{3})?(?: [+-]\d{2}:\d{2})?\]\s*\[[A-Z]+\]", RegexOptions.IgnoreCase | RegexOptions.Compiled));

	// Regex to extract the message content without the log pattern - handles extra brackets
	private static readonly Lazy<Regex> MessageExtractRegex = new(() =>
		new Regex(@"^\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}(?:\.\d{3})?(?: [+-]\d{2}:\d{2})?\]\s*\[[A-Z]+\]\s*(?:\[[^\]]*\])?\s*(?<message>.*)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline));

	// Maximum number of lines to combine for a single log entry
	private const int MaxLinesPerLogEntry = 100;

	private static readonly Lazy<Regex[]> DefaultFallbackTimestampRegexes = new(() => new[]
	{
		new Regex(@"(\d{4}-\d{2}-\d{2}[\sT]\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?(?:Z|[+-]\d{2}:?\d{2})?)", RegexOptions.Compiled),
		new Regex(@"(\d{2}/\d{2}/\d{4} \d{2}:\d{2}:\d{2})", RegexOptions.Compiled),
		new Regex(@"(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})", RegexOptions.Compiled)
	});

	/// <summary>
	/// Initializes a new instance of the <see cref="LogsProviderService"/> class.
	/// </summary>
	/// <param name="logger">Logger instance for logging.</param>
	/// <param name="configuration">Configuration instance to read Serilog settings.</param>
	public LogsProviderService(ILogger<LogsProviderService> logger, IConfiguration configuration)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
	}

	/// <inheritdoc/>
	public async Task<LogsResponse> GetLogsAsync(string? level = null, int limit = 100, int offset = 0,
		DateTime? fromDate = null, DateTime? toDate = null, string? searchTerm = null)
	{
		var (logsDirectory, logPatterns) = GetLogPathsFromSerilogConfig();

		try
		{
			_logger.LogInformation("Getting logs with level: {Level}, limit: {Limit}, offset: {Offset}, searchTerm: {SearchTerm}",
				level ?? "all", limit, offset, searchTerm ?? "none");

			// For better performance with large log files, we'll process files in reverse order
			// and stop when we have enough results
			var results = new List<LogEntry>();
			var totalProcessed = 0;
			var skipped = 0;

			_logger.LogDebug("Checking logs directory: {LogsDirectory}", logsDirectory);
			_logger.LogDebug("Using patterns: {Patterns}", string.Join(", ", logPatterns));

			if (Directory.Exists(logsDirectory))
			{
				_logger.LogDebug("Directory exists, searching for files...");

				// Use EnumerateFiles for better memory efficiency
				var logFiles = logPatterns
					.SelectMany(pattern =>
					{
						var matchingFiles = Directory.EnumerateFiles(logsDirectory, pattern);
						_logger.LogDebug("Pattern '{Pattern}' searching...", pattern);
						return matchingFiles;
					})
					.OrderByDescending(File.GetLastWriteTime);

				foreach (var logFile in logFiles)
				{
					try
					{
						// Use streaming approach with smart offset and limit handling
						var result = await ProcessLogFileOptimizedAsync(logFile, results, totalProcessed, skipped,
							level, fromDate, toDate, searchTerm, offset, limit);

						totalProcessed = result.TotalProcessed;
						skipped = result.Skipped;

						// Early exit if we have enough results
						if (results.Count >= limit || result.ShouldStop)
							break;
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Failed to read log file: {LogFile}", logFile);
					}
				}
			}
			else
			{
				_logger.LogWarning("Logs directory does not exist: {LogsDirectory}", logsDirectory);
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

			var response = new LogsResponse
			{
				TotalLogs = totalCount,
				Level = level ?? "all",
				Limit = limit,
				Logs = results,
				RequestId = string.Empty, // Will be set by caller
			};

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
		var (logsDirectory, logPatterns) = GetLogPathsFromSerilogConfig();
		var count = 0;

		try
		{
			if (Directory.Exists(logsDirectory))
			{
				var logFiles = logPatterns
					.SelectMany(pattern => Directory.EnumerateFiles(logsDirectory, pattern));

				foreach (var logFile in logFiles)
				{
					try
					{
						// Use streaming approach for counting to avoid loading entire file into memory
						using var fileStream = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
						using var reader = new StreamReader(fileStream);

						var logEntries = await ParseLogEntriesAsync(reader);
						foreach (var logEntry in logEntries)
						{
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
	public Task<IEnumerable<LogFileInfo>> GetLogFilesAsync()
	{
		var (logsDirectory, logPatterns) = GetLogPathsFromSerilogConfig();
		var files = new List<LogFileInfo>();

		try
		{
			if (Directory.Exists(logsDirectory))
			{
				var logFiles = logPatterns
					.SelectMany(pattern => Directory.EnumerateFiles(logsDirectory, pattern));

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

			return Task.FromResult<IEnumerable<LogFileInfo>>(files.OrderByDescending(f => f.LastModified));
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving log files");
			return Task.FromResult<IEnumerable<LogFileInfo>>(Enumerable.Empty<LogFileInfo>());
		}
	}

	/// <inheritdoc/>
	public async IAsyncEnumerable<LogEntry> StreamLogsAsync(string? level = null, int initialLimit = 50,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var (logsDirectory, logPatterns) = GetLogPathsFromSerilogConfig();

		if (!Directory.Exists(logsDirectory))
		{
			_logger.LogWarning("Logs directory does not exist: {LogsDirectory}", logsDirectory);
			yield break;
		}

		_logger.LogInformation("Starting continuous log streaming for directory: {LogsDirectory} with level: {Level}, initialLimit: {InitialLimit}",
			logsDirectory, level ?? "all", initialLimit);

		// First, yield existing recent log entries
		await foreach (var existingEntry in GetRecentLogEntriesAsync(logsDirectory, logPatterns, level, initialLimit, cancellationToken))
		{
			if (cancellationToken.IsCancellationRequested)
				yield break;

			yield return existingEntry;
		}

		// Then start continuous monitoring for new log entries
		await foreach (var newEntry in PollLogFilesAsync(logsDirectory, logPatterns, level, cancellationToken))
		{
			if (cancellationToken.IsCancellationRequested)
				yield break;

			yield return newEntry;
		}
	}

	/// <inheritdoc/>
	public async IAsyncEnumerable<IEnumerable<LogEntry>> StreamLogBatchesAsync(string? level = null, int initialLimit = 50, int batchSize = 10,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var (logsDirectory, logPatterns) = GetLogPathsFromSerilogConfig();

		if (!Directory.Exists(logsDirectory))
		{
			_logger.LogWarning("Logs directory does not exist: {LogsDirectory}", logsDirectory);
			yield break;
		}

		_logger.LogInformation("Starting batch log streaming for directory: {LogsDirectory} with level: {Level}, initialLimit: {InitialLimit}, batchSize: {BatchSize}",
			logsDirectory, level ?? "all", initialLimit, batchSize);

		var currentBatch = new List<LogEntry>();

		// First, yield existing recent log entries in batches
		await foreach (var existingEntry in GetRecentLogEntriesAsync(logsDirectory, logPatterns, level, initialLimit, cancellationToken))
		{
			if (cancellationToken.IsCancellationRequested)
				yield break;

			currentBatch.Add(existingEntry);

			if (currentBatch.Count >= batchSize)
			{
				yield return currentBatch.ToList();
				currentBatch.Clear();
			}
		}

		// Yield any remaining entries from initial load
		if (currentBatch.Count > 0)
		{
			yield return currentBatch.ToList();
			currentBatch.Clear();
		}

		// Then start continuous monitoring for new log entries with timeout-based batching
		var lastBatchTime = DateTime.UtcNow;
		const int batchTimeoutMs = 5000; // 5 seconds timeout

		await foreach (var newEntry in PollLogFilesAsync(logsDirectory, logPatterns, level, cancellationToken))
		{
			if (cancellationToken.IsCancellationRequested)
				yield break;

			currentBatch.Add(newEntry);

			// Send batch if it's full OR if 5 seconds have passed since last batch
			var now = DateTime.UtcNow;
			var timeSinceLastBatch = (now - lastBatchTime).TotalMilliseconds;

			if (currentBatch.Count >= batchSize || timeSinceLastBatch >= batchTimeoutMs)
			{
				if (currentBatch.Count > 0)
				{
					_logger.LogDebug("Sending batch of {Count} entries (timeout: {TimedOut})",
						currentBatch.Count, timeSinceLastBatch >= batchTimeoutMs);
					yield return currentBatch.ToList();
					currentBatch.Clear();
					lastBatchTime = now;
				}
			}
		}

		// Send any remaining entries in the final batch
		if (currentBatch.Count > 0)
		{
			_logger.LogDebug("Sending final batch of {Count} entries", currentBatch.Count);
			yield return currentBatch.ToList();
		}
	}



	private static string ExtractTimestamp(string logLine)
	{
		var match = DefaultTimestampRegex.Value.Match(logLine);
		if (match.Success && match.Groups["timestamp"].Success)
		{
			// Return full timestamp
			return match.Groups["timestamp"].Value;
		}

		// Fallback to previous formats if needed
		foreach (var regex in DefaultFallbackTimestampRegexes.Value)
		{
			var fallbackMatch = regex.Match(logLine);
			if (fallbackMatch.Success)
				return fallbackMatch.Value;
		}
		return DateTime.UtcNow.ToString("HH:mm:ss");
	}

	private static string ExtractLogLevel(string logLine)
	{
		var match = DefaultLogLevelRegex.Value.Match(logLine);
		if (match.Success && match.Groups["level"].Success)
		{
			return match.Groups["level"].Value.ToUpperInvariant();
		}
		return "INF";
	}

	/// <summary>
	/// Extracts the actual message content from a log line, removing the timestamp and log level prefix
	/// </summary>
	private static string ExtractMessageContent(string logLine)
	{
		var match = MessageExtractRegex.Value.Match(logLine);
		if (match.Success && match.Groups["message"].Success)
		{
			return match.Groups["message"].Value;
		}
		// If no pattern match, return the line as-is (for continuation lines or malformed entries)
		return logLine;
	}

	private static DateTime TryParseTimestamp(string timestamp)
	{
		// Try various timestamp formats
		var formats = new[]
		{
			"yyyy-MM-dd HH:mm:ss.fff zzz",   // New Serilog format with timezone
			"yyyy-MM-dd HH:mm:ss.fff",       // Without timezone
			"HH:mm:ss",                      // Time only (test format)
			"HH:mm:ss.fff",                  // Time with milliseconds
			"yyyy-MM-dd HH:mm:ss",
			"yyyy-MM-ddTHH:mm:ss",
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
				// If we only parsed time, add today's date
				if (format.StartsWith("HH:"))
				{
					var today = DateTime.Today;
					return new DateTime(today.Year, today.Month, today.Day, result.Hour, result.Minute, result.Second, result.Millisecond);
				}
				return result;
			}
		}

		// Fallback to general parsing
		if (DateTime.TryParse(timestamp, out var fallbackResult))
			return fallbackResult;

		return DateTime.UtcNow;
	}

	/// <summary>
	/// Extracts log directory and file patterns from Serilog configuration.
	/// Uses AppContext.BaseDirectory for relative paths, absolute paths for Docker volumes.
	/// </summary>
	/// <returns>A tuple containing the logs directory path and array of log file patterns.</returns>
	private (string LogsDirectory, string[] LogPatterns) GetLogPathsFromSerilogConfig()
	{
		var logPaths = new List<string>();
		var serilogWriteTo = _configuration.GetSection("Serilog:WriteTo").GetChildren();

		foreach (var sink in serilogWriteTo)
		{
			var sinkName = sink.GetValue<string>("Name");
			if (sinkName == "File")
			{
				var filePath = sink.GetValue<string>("Args:path");
				if (!string.IsNullOrEmpty(filePath))
				{
					logPaths.Add(filePath);
				}
			}
		}

		// If no Serilog file sinks found, use defaults with AppContext.BaseDirectory
		if (logPaths.Count == 0)
		{
			return (Path.Combine(AppContext.BaseDirectory, "Logs"), new[] { "jiro-detailed_*.log", "jiro-errors_*.log", "jiro_*.log", "jiro_*.txt" });
		}

		// Extract directory from first log path and resolve it correctly
		var firstLogPath = logPaths[0];
		var directory = Path.GetDirectoryName(firstLogPath);

		string logsDirectory;
		if (string.IsNullOrEmpty(directory))
		{
			// If no directory in path, use default Logs folder relative to app
			logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
		}
		else if (Path.IsPathRooted(directory))
		{
			// Absolute path - use as is (for example Docker volumes)
			logsDirectory = directory;
		}
		else
		{
			// Relative path - combine with AppContext.BaseDirectory
			logsDirectory = Path.Combine(AppContext.BaseDirectory, directory);
		}

		// Create patterns from file paths, handling Serilog rolling patterns correctly
		var patterns = logPaths
			.Select(path => Path.GetFileName(path))
			.Where(fileName => !string.IsNullOrEmpty(fileName))
			.Select(fileName =>
			{
				// Convert Serilog rolling patterns and common date patterns:
				// "jiro-.log" -> "jiro-*.log" (handles date rolling)
				// "jiro-detailed_.txt" -> "jiro-detailed_*.txt"
				// "jiro_{Date}.log" -> "jiro_*.log"
				// "{Date}" -> "*", "{Hour}" -> "*", etc.
				var pattern = fileName!
					.Replace("_.", "_*.")
					.Replace("{Date}", "*")
					.Replace("{Hour}", "*")
					.Replace("{HalfHour}", "*")
					.Replace("{date}", "*")  // lowercase variant
					.Replace("{hour}", "*")  // lowercase variant
					.Replace("_{", "_*")     // Handle patterns like jiro_{date}.log -> jiro_*.log
					.Replace("}", "");       // Remove remaining braces

				// Special handling for Serilog rolling file pattern like "jiro-.log"
				if (pattern.Contains("-."))
				{
					pattern = pattern.Replace("-.", "-*.");
				}

				return pattern;
			})
			.ToArray();

		// If no valid patterns, use defaults
		if (patterns.Length == 0)
		{
			patterns = new[] { "jiro-detailed_*.log", "jiro-errors_*.log", "jiro_*.log", "jiro_*.txt" };
		}

		_logger.LogDebug("Resolved logs directory: {LogsDirectory}, patterns: {Patterns}",
			logsDirectory, string.Join(", ", patterns));

		return (logsDirectory, patterns);
	}

	/// <summary>
	/// Result container for log file processing
	/// </summary>
	private class LogProcessingResult
	{
		public int TotalProcessed { get; set; }
		public int Skipped { get; set; }
		public bool ShouldStop { get; set; }
	}

	/// <summary>
	/// Optimized method to process a single log file with smart offset and limit handling.
	/// Uses streaming approach with FileShare.ReadWrite for better concurrency.
	/// </summary>
	private async Task<LogProcessingResult> ProcessLogFileOptimizedAsync(string logFile, List<LogEntry> results,
		int currentTotalProcessed, int currentSkipped, string? level, DateTime? fromDate, DateTime? toDate,
		string? searchTerm, int offset, int limit)
	{
		using var fileStream = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		using var reader = new StreamReader(fileStream);

		// Parse log entries (handles multi-line logs)
		var logEntries = (await ParseLogEntriesAsync(reader)).ToList();
		var fileName = Path.GetFileName(logFile);

		// Add file name to each entry
		foreach (var entry in logEntries)
		{
			entry.File = fileName;
		}

		var totalProcessed = currentTotalProcessed;
		var skipped = currentSkipped;

		// Process entries in reverse order (newest first) for better performance
		for (int i = logEntries.Count - 1; i >= 0; i--)
		{
			var logEntry = logEntries[i];

			// Parse timestamp for accurate filtering
			var parsedTimestamp = TryParseTimestamp(logEntry.Timestamp);

			// Apply filters first to avoid unnecessary processing
			if (!PassesFilters(logEntry, parsedTimestamp, level, fromDate, toDate, searchTerm))
				continue;

			totalProcessed++;

			// Smart offset handling - skip entries if we're not at the requested offset yet
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
			else
			{
				// We have enough results, exit early for maximum efficiency
				return new LogProcessingResult
				{
					TotalProcessed = totalProcessed,
					Skipped = skipped,
					ShouldStop = true
				};
			}
		}

		return new LogProcessingResult
		{
			TotalProcessed = totalProcessed,
			Skipped = skipped,
			ShouldStop = false
		};
	}

	private static bool PassesFilters(LogEntry logEntry, DateTime parsedTimestamp,
		string? level, DateTime? fromDate, DateTime? toDate, string? searchTerm)
	{
		// Level filter - skip filtering if level is null, empty, or "all"
		if (!string.IsNullOrEmpty(level) &&
			!level.Equals("all", StringComparison.OrdinalIgnoreCase) &&
			!IsLogLevelMatch(logEntry.Level, level))
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

	/// <summary>
	/// Matches log levels supporting both abbreviated (INF, DBG, WRN, ERR) and full names (Information, Debug, Warning, Error)
	/// </summary>
	private static bool IsLogLevelMatch(string extractedLevel, string requestedLevel)
	{
		// Direct match (case-insensitive)
		if (extractedLevel.Equals(requestedLevel, StringComparison.OrdinalIgnoreCase))
			return true;

		// Map full level names to abbreviations
		var levelMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "Information", "INF" },
			{ "Debug", "DBG" },
			{ "Warning", "WRN" },
			{ "Error", "ERR" },
			{ "Fatal", "FTL" },
			{ "Verbose", "VRB" },
			{ "Trace", "TRC" }
		};

		// Check if requested level maps to extracted level
		return levelMapping.TryGetValue(requestedLevel, out var mappedLevel) &&
			   extractedLevel.Equals(mappedLevel, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Parses log entries from a stream reader, handling multi-line logs properly
	/// </summary>
	private async Task<IEnumerable<LogEntry>> ParseLogEntriesAsync(StreamReader reader)
	{
		var entries = new List<LogEntry>();
		var currentEntry = new StringBuilder();
		string? currentTimestamp = null;
		string? currentLevel = null;
		string? line;
		int linesInCurrentEntry = 0;

		while ((line = await reader.ReadLineAsync()) != null)
		{
			// Check if this line starts a new log entry
			if (LogEntryStartRegex.Value.IsMatch(line))
			{
				// Save the previous entry if exists
				if (currentEntry.Length > 0 && currentTimestamp != null)
				{
					entries.Add(new LogEntry
					{
						Timestamp = currentTimestamp,
						Level = currentLevel ?? "INF",
						Message = currentEntry.ToString().TrimEnd()
					});
				}

				// Start new entry
				currentEntry.Clear();
				currentEntry.AppendLine(ExtractMessageContent(line));
				currentTimestamp = ExtractTimestamp(line);
				currentLevel = ExtractLogLevel(line);
				linesInCurrentEntry = 1;
			}
			else if (currentEntry.Length > 0)
			{
				// This is a continuation of the current log entry
				if (linesInCurrentEntry < MaxLinesPerLogEntry)
				{
					currentEntry.AppendLine(line);
					linesInCurrentEntry++;
				}
			}
			// If we don't have a current entry and this line doesn't start a new one, skip it
		}

		// Don't forget the last entry
		if (currentEntry.Length > 0 && currentTimestamp != null)
		{
			entries.Add(new LogEntry
			{
				Timestamp = currentTimestamp,
				Level = currentLevel ?? "INF",
				Message = currentEntry.ToString().TrimEnd()
			});
		}

		return entries;
	}

	/// <summary>
	/// Parses log entries from a list of lines, handling multi-line logs properly
	/// </summary>
	private List<LogEntry> ParseLogEntriesFromLines(List<string> lines, string fileName)
	{
		var entries = new List<LogEntry>();
		var currentEntry = new StringBuilder();
		string? currentTimestamp = null;
		string? currentLevel = null;
		int linesInCurrentEntry = 0;

		foreach (var line in lines)
		{
			// Check if this line starts a new log entry
			if (LogEntryStartRegex.Value.IsMatch(line))
			{
				// Save the previous entry if exists
				if (currentEntry.Length > 0 && currentTimestamp != null)
				{
					entries.Add(new LogEntry
					{
						File = fileName,
						Timestamp = currentTimestamp,
						Level = currentLevel ?? "INF",
						Message = currentEntry.ToString().TrimEnd()
					});
				}

				// Start new entry
				currentEntry.Clear();
				currentEntry.AppendLine(ExtractMessageContent(line));
				currentTimestamp = ExtractTimestamp(line);
				currentLevel = ExtractLogLevel(line);
				linesInCurrentEntry = 1;
			}
			else if (currentEntry.Length > 0)
			{
				// This is a continuation of the current log entry
				if (linesInCurrentEntry < MaxLinesPerLogEntry)
				{
					currentEntry.AppendLine(line);
					linesInCurrentEntry++;
				}
			}
			// If we don't have a current entry and this line doesn't start a new one, skip it
		}

		// Don't forget the last entry
		if (currentEntry.Length > 0 && currentTimestamp != null)
		{
			entries.Add(new LogEntry
			{
				File = fileName,
				Timestamp = currentTimestamp,
				Level = currentLevel ?? "INF",
				Message = currentEntry.ToString().TrimEnd()
			});
		}

		return entries;
	}

	/// <summary>
	/// Gets recent log entries from existing log files for initial streaming
	/// </summary>
	private async IAsyncEnumerable<LogEntry> GetRecentLogEntriesAsync(string logsDirectory, string[] logPatterns,
		string? level, int limit, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		IEnumerable<LogEntry> recentEntries;

		try
		{
			recentEntries = await GetRecentLogEntriesInternalAsync(logsDirectory, logPatterns, level, limit, cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting recent log entries");
			yield break;
		}

		// Return the entries
		foreach (var entry in recentEntries)
		{
			if (cancellationToken.IsCancellationRequested)
				yield break;
			yield return entry;
		}
	}

	/// <summary>
	/// Internal method to get recent log entries without yield in try-catch
	/// </summary>
	private async Task<List<LogEntry>> GetRecentLogEntriesInternalAsync(string logsDirectory, string[] logPatterns,
		string? level, int limit, CancellationToken cancellationToken)
	{
		var recentEntries = new List<LogEntry>();

		// Get the most recent log file
		var logFiles = logPatterns
			.SelectMany(pattern => Directory.EnumerateFiles(logsDirectory, pattern))
			.OrderByDescending(f => File.GetLastWriteTime(f))
			.Take(2); // Take 2 files in case we need to look at previous file

		foreach (var logFile in logFiles)
		{
			if (cancellationToken.IsCancellationRequested)
				break;

			try
			{
				using var fileStream = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				using var reader = new StreamReader(fileStream);

				var entries = (await ParseLogEntriesAsync(reader)).ToList();
				var fileName = Path.GetFileName(logFile);

				// Add file name and filter by level
				foreach (var entry in entries)
				{
					entry.File = fileName;
				}

				// Take the last entries and filter by level
				var filteredEntries = entries
					.Where(e => string.IsNullOrEmpty(level) ||
							level.Equals("all", StringComparison.OrdinalIgnoreCase) ||
							IsLogLevelMatch(e.Level, level))
					.TakeLast(limit)
					.ToList();

				recentEntries.AddRange(filteredEntries);

				// If we have enough entries, break
				if (recentEntries.Count >= limit)
					break;
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Error reading recent entries from log file: {LogFile}", logFile);
			}
		}

		return recentEntries.TakeLast(limit).ToList();
	}

	/// <summary>
	/// Polls log files for changes and streams new entries as they appear
	/// </summary>
	private async IAsyncEnumerable<LogEntry> PollLogFilesAsync(string logsDirectory, string[] logPatterns,
		string? level, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		var filePositions = new Dictionary<string, long>();
		var pendingLines = new Dictionary<string, List<string>>();

		_logger.LogInformation("Starting log file polling for patterns: {Patterns}", string.Join(", ", logPatterns));

		// Initialize positions for existing files
		var existingFiles = logPatterns
			.SelectMany(pattern => Directory.EnumerateFiles(logsDirectory, pattern));

		foreach (var file in existingFiles)
		{
			try
			{
				var fileInfo = new FileInfo(file);
				filePositions[file] = fileInfo.Length;
				pendingLines[file] = new List<string>();
				_logger.LogDebug("Initialized position {Position} for file: {File}", fileInfo.Length, file);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Error initializing position for file: {File}", file);
			}
		}

		// Process file changes
		while (!cancellationToken.IsCancellationRequested)
		{
			var hasNewEntries = false;

			// Check all monitored files for changes
			foreach (var filePath in filePositions.Keys.ToList())
			{
				if (cancellationToken.IsCancellationRequested)
					yield break;

				if (!File.Exists(filePath))
				{
					// File may have been rotated, remove from tracking
					filePositions.Remove(filePath);
					pendingLines.Remove(filePath);
					continue;
				}

				// Check if file has new content
				long currentSize = 0;
				long lastPosition = 0;
				bool hasFileGrown = false;

				try
				{
					var currentFileInfo = new FileInfo(filePath);
					currentSize = currentFileInfo.Length;
					lastPosition = filePositions[filePath];
					hasFileGrown = currentSize > lastPosition;

					if (currentSize < lastPosition)
					{
						// File was truncated or rotated, reset position
						filePositions[filePath] = 0;
						pendingLines[filePath].Clear();
						_logger.LogInformation("File appears to have been rotated: {File}", filePath);
					}
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Error checking file info: {File}", filePath);
					continue;
				}

				// Read new content if file has grown
				if (hasFileGrown)
				{
					await foreach (var entry in ReadNewLogContentAsync(filePath, lastPosition, level, pendingLines[filePath], cancellationToken))
					{
						hasNewEntries = true;
						yield return entry;
					}

					filePositions[filePath] = currentSize;
				}
			}

			// Check for new files that match our patterns
			var currentFiles = logPatterns
				.SelectMany(pattern => Directory.EnumerateFiles(logsDirectory, pattern))
				.ToHashSet();

			foreach (var newFile in currentFiles.Where(f => !filePositions.ContainsKey(f)))
			{
				try
				{
					var fileInfo = new FileInfo(newFile);
					filePositions[newFile] = 0; // Start from beginning for new files
					pendingLines[newFile] = new List<string>();
					_logger.LogInformation("New log file detected: {File}", newFile);
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Error adding new file to monitoring: {File}", newFile);
				}
			}

			// Wait before next check if no new entries
			if (!hasNewEntries)
			{
				await Task.Delay(500, cancellationToken); // Check every 500ms
			}
		}

		_logger.LogInformation("Log file polling stopped");
	}

	/// <summary>
	/// Reads new content from a log file since the last position
	/// </summary>
	private async IAsyncEnumerable<LogEntry> ReadNewLogContentAsync(string filePath, long fromPosition,
		string? level, List<string> pendingLines, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		IEnumerable<LogEntry> entries;

		try
		{
			entries = await ReadNewLogContentInternalAsync(filePath, fromPosition, level, pendingLines, cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Error reading new log content from: {File}", filePath);
			yield break;
		}

		// Yield the entries
		foreach (var entry in entries)
		{
			if (cancellationToken.IsCancellationRequested)
				yield break;
			yield return entry;
		}
	}

	/// <summary>
	/// Internal method to read new log content without yield in try-catch
	/// </summary>
	private async Task<List<LogEntry>> ReadNewLogContentInternalAsync(string filePath, long fromPosition,
		string? level, List<string> pendingLines, CancellationToken cancellationToken)
	{
		using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		fileStream.Seek(fromPosition, SeekOrigin.Begin);

		using var reader = new StreamReader(fileStream);
		var fileName = Path.GetFileName(filePath);
		var newLines = new List<string>();

		string? line;
		while ((line = await reader.ReadLineAsync()) != null)
		{
			if (cancellationToken.IsCancellationRequested)
				break;

			newLines.Add(line);
		}

		if (newLines.Count == 0)
			return new List<LogEntry>();

		// Combine with pending lines from previous reads
		var allLines = new List<string>(pendingLines);
		allLines.AddRange(newLines);
		pendingLines.Clear();

		// Parse complete log entries
		var entries = ParseLogEntriesFromLines(allLines, fileName);

		// The last entry might be incomplete if the line doesn't end with a newline
		// Keep the last incomplete entry for next read
		if (entries.Count > 0)
		{
			var lastEntry = entries[entries.Count - 1];
			if (!newLines[newLines.Count - 1].EndsWith('\n') && !newLines[newLines.Count - 1].EndsWith('\r'))
			{
				// Last line might be incomplete, store it for next read
				var lastLineIndex = allLines.FindLastIndex(l => l == newLines[newLines.Count - 1]);
				if (lastLineIndex >= 0)
				{
					// Keep lines from the last log entry start for next read
					for (int i = lastLineIndex; i < allLines.Count; i++)
					{
						if (LogEntryStartRegex.Value.IsMatch(allLines[i]))
						{
							// Found start of last entry, keep remaining lines
							pendingLines.AddRange(allLines.Skip(i));
							entries.RemoveAt(entries.Count - 1);
							break;
						}
					}
				}
			}
		}

		// Filter entries by level
		var filteredEntries = new List<LogEntry>();
		foreach (var entry in entries)
		{
			if (cancellationToken.IsCancellationRequested)
				break;

			// Apply level filter
			if (string.IsNullOrEmpty(level) ||
				level.Equals("all", StringComparison.OrdinalIgnoreCase) ||
				IsLogLevelMatch(entry.Level, level))
			{
				filteredEntries.Add(entry);
			}
		}

		return filteredEntries;
	}
}

/// <summary>
/// Represents metadata information about a log file in the system.
/// Used for log file discovery, management, and providing file details to clients.
/// </summary>
public class LogFileInfo
{
	/// <summary>
	/// Gets or sets the name of the log file including extension.
	/// </summary>
	/// <value>
	/// The file name only (e.g., "app-20241123.log"), without the full path.
	/// </value>
	/// <example>
	/// "app-20241123.log", "errors.log", "debug-20241123.txt"
	/// </example>
	public string FileName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the full absolute path to the log file.
	/// </summary>
	/// <value>
	/// The complete file system path including directory and filename.
	/// </value>
	/// <example>
	/// "/app/logs/app-20241123.log", "C:\Logs\errors.log"
	/// </example>
	public string FilePath { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the size of the log file in bytes.
	/// </summary>
	/// <value>
	/// The file size as reported by the file system. Returns 0 if the file cannot be accessed.
	/// </value>
	/// <remarks>
	/// This value represents the current size at the time of metadata collection.
	/// For active log files, this size may change as new entries are written.
	/// </remarks>
	public long SizeBytes { get; set; }

	/// <summary>
	/// Gets or sets the date and time when the log file was last modified.
	/// </summary>
	/// <value>
	/// The last write time as reported by the file system, typically in UTC.
	/// </value>
	/// <remarks>
	/// For active log files, this timestamp updates each time new log entries are written.
	/// Useful for determining which files contain the most recent log data.
	/// </remarks>
	public DateTime LastModified { get; set; }

	/// <summary>
	/// Gets or sets the date and time when the log file was created.
	/// </summary>
	/// <value>
	/// The creation time as reported by the file system, typically in UTC.
	/// </value>
	/// <remarks>
	/// For log files created through log rotation, this represents when the specific
	/// file was first created, not when logging started for the application.
	/// </remarks>
	public DateTime Created { get; set; }
}
