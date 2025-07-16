namespace Jiro.App.Models;

/// <summary>
/// Represents a log entry.
/// </summary>
public class LogEntry
{
    /// <summary>
    /// Gets or sets the file name of the log.
    /// </summary>
    public string File { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp of the log entry.
    /// </summary>
    public string Timestamp { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the log level.
    /// </summary>
    public string Level { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the log message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
