using Jiro.Core.Services.System.Models;

namespace Jiro.App.Models;

/// <summary>
/// Represents the response for keepalive acknowledgment.
/// </summary>
public class KeepaliveResponse : SyncResponse
{
    /// <summary>
    /// Gets or sets the timestamp of the acknowledgment.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the status of the acknowledgment.
    /// </summary>
    public string Status { get; set; } = "acknowledged";
}
