namespace Jiro.App.Models;

/// <summary>
/// Represents a theme.
/// </summary>
public class Theme
{
    /// <summary>
    /// Gets or sets the theme name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the theme description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the theme color scheme.
    /// </summary>
    public string ColorScheme { get; set; } = string.Empty;
}
