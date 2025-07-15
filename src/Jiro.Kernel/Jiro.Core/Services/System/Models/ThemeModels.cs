namespace Jiro.Core.Services.System.Models;

/// <summary>
/// Theme information
/// </summary>
public class Theme
{
    public string Name { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ThemeColors Colors { get; set; } = new();
    public bool IsActive { get; set; }
}

/// <summary>
/// Theme color information
/// </summary>
public class ThemeColors
{
    public string Primary { get; set; } = string.Empty;
    public string Secondary { get; set; } = string.Empty;
    public string Background { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Theme response containing available themes
/// </summary>
public class ThemeResponse
{
    public int TotalThemes { get; set; }
    public List<Theme> Themes { get; set; } = [];
}
