using Jiro.Core.Services.System.Models;

namespace Jiro.App.Models;

/// <summary>
/// Represents the response for themes.
/// </summary>
public class ThemesResponse : SyncResponse
{
    /// <summary>
    /// Gets or sets the list of available themes.
    /// </summary>
    public List<Theme> Themes { get; set; } = new();
}
