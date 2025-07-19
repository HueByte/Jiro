using System.Text.RegularExpressions;

using Jiro.Core.Services.System.Models;
using Jiro.Shared.Websocket.Requests;
using Jiro.Shared.Websocket.Responses;

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
	public async Task<LogsResponse> GetLogsAsync(string? level = null, int limit = 100)
	{
		try
		{
			_logger.LogInformation("Getting logs with level: {Level}, limit: {Limit}", level ?? "all", limit);

			var logs = new List<LogEntry>();
			var logsDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");

			if (Directory.Exists(logsDirectory))
			{
				var logFiles = Directory.GetFiles(logsDirectory, "*.txt")
					.OrderByDescending(f => File.GetLastWriteTime(f))
					.Take(5);

				foreach (var logFile in logFiles)
				{
					try
					{
						var lines = await File.ReadAllLinesAsync(logFile);
						var relevantLines = lines.AsEnumerable();

						// Filter by level if specified
						if (!string.IsNullOrEmpty(level))
						{
							relevantLines = relevantLines.Where(line =>
								line.Contains($"[{level}]", StringComparison.OrdinalIgnoreCase));
						}

						// Take only the most recent entries
						relevantLines = relevantLines.TakeLast(limit / Math.Max(1, logFiles.Count())).ToList();

						foreach (var line in relevantLines)
						{
							logs.Add(new LogEntry
							{
								File = Path.GetFileName(logFile),
								Timestamp = ExtractTimestamp(line),
								Level = ExtractLogLevel(line),
								Message = line
							});
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Failed to read log file: {LogFile}", logFile);
					}
				}
			}

			// Sort by timestamp and take the limit
			var result = logs
				.OrderByDescending(log => log.Timestamp)
				.Take(limit)
				.ToList();

			return new LogsResponse
			{
				TotalLogs = result.Count,
				Level = level ?? "all",
				Limit = limit,
				Logs = result
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving logs");
			throw;
		}
	}

	private static string ExtractTimestamp(string logLine)
	{
		var timestampMatch = Regex.Match(logLine, @"(\d{4}-\d{2}-\d{2}[\sT]\d{2}:\d{2}:\d{2})");
		return timestampMatch.Success ? timestampMatch.Value : DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
	}

	private static string ExtractLogLevel(string logLine)
	{
		var levels = new[] { "TRACE", "DEBUG", "INFO", "WARN", "ERROR", "FATAL" };
		foreach (var level in levels)
		{
			if (logLine.Contains($"[{level}]", StringComparison.OrdinalIgnoreCase))
			{
				return level;
			}
		}
		return "INFO";
	}
}
