using Jiro.Core.Services.System.Models;

namespace Jiro.App.Models;

/// <summary>
/// Represents the response for errors.
/// </summary>
public class ErrorResponse : SyncResponse
{
    /// <summary>
    /// Gets or sets the command name associated with the error.
    /// </summary>
    public string CommandName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}
