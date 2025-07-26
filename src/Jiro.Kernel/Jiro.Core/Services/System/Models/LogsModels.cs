namespace Jiro.Core.Services.System.Models;

/// <summary>
/// Represents a single log entry for internal processing
/// </summary>
public class LogEntry
{
	/// <summary>
	/// Gets or sets the file name where the log entry originated.
	/// </summary>
	public string File { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the timestamp when the log entry was created.
	/// </summary>
	public string Timestamp { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the log level (e.g., Debug, Info, Warning, Error).
	/// </summary>
	public string Level { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the log message content.
	/// </summary>
	public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response object for log queries with pagination support
/// </summary>
public class LogsResponse
{
	/// <summary>
	/// Gets or sets the total number of log entries available.
	/// </summary>
	public int TotalLogs { get; set; }

	/// <summary>
	/// Gets or sets the log level filter that was applied to the query.
	/// </summary>
	public string Level { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the maximum number of log entries to return.
	/// </summary>
	public int Limit { get; set; }

	/// <summary>
	/// Gets or sets the number of log entries to skip from the beginning.
	/// </summary>
	public int Offset { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether there are more log entries available beyond the current page.
	/// </summary>
	public bool HasMore { get; set; }

	/// <summary>
	/// Gets or sets the collection of log entries returned by the query.
	/// </summary>
	public List<LogEntry> Logs { get; set; } = new();

	/// <summary>
	/// Gets or sets the unique identifier for this log query request.
	/// </summary>
	public string RequestId { get; set; } = string.Empty;
}
