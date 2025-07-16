namespace Jiro.App.Models;

/// <summary>
/// Represents the response for configuration updates.
/// </summary>
public class ConfigUpdateResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the update was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the message associated with the update.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the keys received during the update.
    /// </summary>
    public string[] ReceivedKeys { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets additional notes regarding the update.
    /// </summary>
    public string Note { get; set; } = string.Empty;
}
