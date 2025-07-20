namespace Jiro.Core.Services.System.Models;

/// <summary>
/// Represents a single log entry for internal processing
/// </summary>
public class LogEntry
{
	public string File { get; set; } = string.Empty;
	public string Timestamp { get; set; } = string.Empty;
	public string Level { get; set; } = string.Empty;
	public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response object for log queries with pagination support
/// </summary>
public class LogsResponse
{
	public int TotalLogs { get; set; }
	public string Level { get; set; } = string.Empty;
	public int Limit { get; set; }
	public int Offset { get; set; }
	public bool HasMore { get; set; }
	public List<LogEntry> Logs { get; set; } = new();
	public string RequestId { get; set; } = string.Empty;
}