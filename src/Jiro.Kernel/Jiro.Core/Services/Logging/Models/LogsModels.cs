namespace Jiro.Core.Services.Logging.Models;

/// <summary>
/// Represents a single log entry
/// </summary>
public class LogEntry
{
    public string File { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response object for log queries
/// </summary>
public class LogsResponse
{
    public int TotalLogs { get; set; }
    public string Level { get; set; } = string.Empty;
    public int Limit { get; set; }
    public List<LogEntry> Logs { get; set; } = new();
}
