using System.Globalization;
using System.Runtime.CompilerServices;
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
		new Regex(@"\[(\d{2}:\d{2}:\d{2}) (VER|DBG|INF|WRN|ERR|FTL)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled));
	
	private static readonly Lazy<Regex> DefaultLogLevelRegex = new(() => 
		new Regex(@"\[\d{2}:\d{2}:\d{2} (VER|DBG|INF|WRN|ERR|FTL)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled));
	
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

			if (Directory.Exists(logsDirectory))
			{
				var logFiles = logPatterns
					.SelectMany(pattern => Directory.GetFiles(logsDirectory, pattern))
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
		var (logsDirectory, logPatterns) = GetLogPathsFromSerilogConfig();
		var count = 0;

		try
		{
			if (Directory.Exists(logsDirectory))
			{
				var logFiles = logPatterns
					.SelectMany(pattern => Directory.GetFiles(logsDirectory, pattern));

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
		var (logsDirectory, logPatterns) = GetLogPathsFromSerilogConfig();
		var files = new List<LogFileInfo>();

		try
		{
			if (Directory.Exists(logsDirectory))
			{
				var logFiles = logPatterns
					.SelectMany(pattern => Directory.GetFiles(logsDirectory, pattern));

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
		var (logsDirectory, logPatterns) = GetLogPathsFromSerilogConfig();

		if (!Directory.Exists(logsDirectory))
			yield break;

		// For now, just yield existing logs (placeholder for future streaming implementation)
		var logFiles = logPatterns
			.SelectMany(pattern => Directory.GetFiles(logsDirectory, pattern))
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
		var match = DefaultTimestampRegex.Value.Match(logLine);
		if (match.Success && match.Groups.Count > 1)
		{
			// Return time part only
			return match.Groups[1].Value;
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
		if (match.Success && match.Groups.Count > 1)
		{
			return match.Groups[1].Value.ToUpperInvariant();
		}
		return "INF";
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

	/// <summary>
	/// Extracts log directory and file patterns from Serilog configuration.
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

		// If no Serilog file sinks found, use defaults
		if (logPaths.Count == 0)
		{
			return (Path.Combine(AppContext.BaseDirectory, "Logs"), new[] { "jiro-detailed_*.txt", "jiro-errors_*.txt" });
		}

		// Extract directory from first log path
		var firstLogPath = logPaths[0];
		var directory = Path.GetDirectoryName(firstLogPath);
		var logsDirectory = string.IsNullOrEmpty(directory) 
			? Path.Combine(AppContext.BaseDirectory, "Logs")
			: Path.IsPathRooted(directory) ? directory : Path.Combine(AppContext.BaseDirectory, directory);

		// Create patterns from file paths
		var patterns = logPaths
			.Select(path => Path.GetFileName(path))
			.Where(fileName => !string.IsNullOrEmpty(fileName))
			.Select(fileName => fileName!.Replace("_.", "_*."))  // Convert Serilog rolling patterns
			.ToArray();

		// If no valid patterns, use defaults
		if (patterns.Length == 0)
		{
			patterns = new[] { "jiro-detailed_*.txt", "jiro-errors_*.txt" };
		}

		return (logsDirectory, patterns);
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
