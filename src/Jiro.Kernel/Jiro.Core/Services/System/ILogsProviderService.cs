using Jiro.Core.Services.System.Models;

namespace Jiro.Core.Services.System;

/// <summary>
/// Service for retrieving system logs with pagination and streaming support
/// </summary>
public interface ILogsProviderService
{
	/// <summary>
	/// Retrieves logs based on level filter, pagination, and other criteria
	/// </summary>
	/// <param name="level">Log level filter (optional)</param>
	/// <param name="limit">Maximum number of logs to retrieve per page</param>
	/// <param name="offset">Number of logs to skip (for pagination)</param>
	/// <param name="fromDate">Start date filter (optional)</param>
	/// <param name="toDate">End date filter (optional)</param>
	/// <param name="searchTerm">Search term to filter messages (optional)</param>
	/// <returns>Log response containing filtered logs with pagination info</returns>
	Task<LogsResponse> GetLogsAsync(string? level = null, int limit = 100, int offset = 0,
		DateTime? fromDate = null, DateTime? toDate = null, string? searchTerm = null);

	/// <summary>
	/// Gets total count of logs matching the specified criteria
	/// </summary>
	/// <param name="level">Log level filter (optional)</param>
	/// <param name="fromDate">Start date filter (optional)</param>
	/// <param name="toDate">End date filter (optional)</param>
	/// <param name="searchTerm">Search term to filter messages (optional)</param>
	/// <returns>Total count of matching log entries</returns>
	Task<int> GetLogCountAsync(string? level = null, DateTime? fromDate = null,
		DateTime? toDate = null, string? searchTerm = null);

	/// <summary>
	/// Gets available log files for selection
	/// </summary>
	/// <returns>List of available log files with metadata</returns>
	Task<IEnumerable<LogFileInfo>> GetLogFilesAsync();

	/// <summary>
	/// Streams logs continuously - first yields the last N logs, then streams new logs in real-time
	/// </summary>
	/// <param name="level">Log level filter (optional)</param>
	/// <param name="initialLimit">Number of recent logs to fetch initially (default 50)</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Async enumerable of log entries</returns>
	IAsyncEnumerable<LogEntry> StreamLogsAsync(string? level = null, int initialLimit = 50, CancellationToken cancellationToken = default);

	/// <summary>
	/// Streams logs in batches - first yields the last N logs in batches, then streams new logs in real-time batches
	/// </summary>
	/// <param name="level">Log level filter (optional)</param>
	/// <param name="initialLimit">Number of recent logs to fetch initially (default 50)</param>
	/// <param name="batchSize">Size of each batch (default 10)</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Async enumerable of log entry batches</returns>
	IAsyncEnumerable<IEnumerable<LogEntry>> StreamLogBatchesAsync(string? level = null, int initialLimit = 50, int batchSize = 10, CancellationToken cancellationToken = default);
}
