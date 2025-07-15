using System.Text.RegularExpressions;

using Jiro.Core.Services.Logging.Models;

using Microsoft.Extensions.Logging;

namespace Jiro.Core.Services.Logging;

/// <summary>
/// Service for retrieving and processing log files
/// </summary>
public class LogsProviderService : ILogsProviderService
{
    private readonly ILogger<LogsProviderService> _logger;

    public LogsProviderService(ILogger<LogsProviderService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves logs with optional filtering and limiting
    /// </summary>
    /// <param name="level">The log level to filter by (optional)</param>
    /// <param name="limit">Maximum number of log entries to return</param>
    /// <returns>A structured response containing log entries</returns>
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
                    if (logs.Count >= limit)
                        break;

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

    /// <summary>
    /// Extracts timestamp from log line
    /// </summary>
    private static string ExtractTimestamp(string logLine)
    {
        // Simple regex to extract timestamp from common log formats
        var timestampMatch = Regex.Match(logLine, @"(\d{4}-\d{2}-\d{2}[\sT]\d{2}:\d{2}:\d{2})");
        return timestampMatch.Success ? timestampMatch.Value : DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// Extracts log level from log line
    /// </summary>
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
