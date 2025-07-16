using Jiro.Core.Services.System.Models;

namespace Jiro.App.Models;

/// <summary>
/// Represents the response for logs.
/// </summary>
public class LogsResponse : SyncResponse
{
    /// <summary>
    /// Gets or sets the total number of logs.
    /// </summary>
    public int TotalLogs { get; set; }

    /// <summary>
    /// Gets or sets the log level.
    /// </summary>
    public string Level { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the limit of logs.
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// Gets or sets the list of logs.
    /// </summary>
    public List<LogEntry> Logs { get; set; } = new();
}
