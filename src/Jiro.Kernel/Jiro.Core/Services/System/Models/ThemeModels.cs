namespace Jiro.Core.Services.System.Models;

/// <summary>
/// Theme information
/// </summary>
public class Theme
{
	/// <summary>
	/// Gets or sets the theme name
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the theme ID
	/// </summary>
	public string Id { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the theme description
	/// </summary>
	public string Description { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the theme colors
	/// </summary>
	public ThemeColors Colors { get; set; } = new();

	/// <summary>
	/// Gets or sets whether the theme is active
	/// </summary>
	public bool IsActive { get; set; }
}

/// <summary>
/// Theme color information
/// </summary>
public class ThemeColors
{
	/// <summary>
	/// Gets or sets the primary color
	/// </summary>
	public string Primary { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the secondary color
	/// </summary>
	public string Secondary { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the background color
	/// </summary>
	public string Background { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the text color
	/// </summary>
	public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Theme response containing available themes
/// </summary>
public class ThemeResponse : SyncResponse
{
	/// <summary>
	/// Gets or sets the total number of themes
	/// </summary>
	public int TotalThemes { get; set; }

	/// <summary>
	/// Gets or sets the list of themes
	/// </summary>
	public List<Theme> Themes { get; set; } = [];
}
