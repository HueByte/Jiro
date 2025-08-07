namespace Jiro.Core.Services.System.Models;

/// <summary>
/// Represents a comprehensive color scheme configuration for theming the application interface.
/// </summary>
public class ColorScheme
{
	/// <summary>
	/// Gets or sets the transparent color value.
	/// </summary>
	public string Transparent { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the white color value.
	/// </summary>
	public string White { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the black color value.
	/// </summary>
	public string Black { get; set; } = string.Empty;

	// Background colors
	/// <summary>
	/// Gets or sets the primary background color for the main application areas.
	/// </summary>
	public string Background { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the secondary background color for content areas and sidebars.
	/// </summary>
	public string BackgroundSecondary { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the tertiary background color for subtle sections and panels.
	/// </summary>
	public string BackgroundTertiary { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the elevated background color for modals, dropdowns, and floating elements.
	/// </summary>
	public string BackgroundElevated { get; set; } = string.Empty;

	// Surface colors
	/// <summary>
	/// Gets or sets the primary surface color for cards and content containers.
	/// </summary>
	public string Surface { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the secondary surface color for nested elements and sub-containers.
	/// </summary>
	public string SurfaceSecondary { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the surface hover color for interactive surface elements.
	/// </summary>
	public string SurfaceHover { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the elevated surface color for tooltips and overlays.
	/// </summary>
	public string SurfaceElevated { get; set; } = string.Empty;

	// Container colors
	/// <summary>
	/// Gets or sets the primary container color for main content wrappers.
	/// </summary>
	public string Container { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the secondary container color for nested content areas.
	/// </summary>
	public string ContainerSecondary { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the container hover color for interactive containers.
	/// </summary>
	public string ContainerHover { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the container active color for selected or pressed containers.
	/// </summary>
	public string ContainerActive { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the elevated container color for raised content sections.
	/// </summary>
	public string ContainerElevated { get; set; } = string.Empty;

	// Border colors
	/// <summary>
	/// Gets or sets the primary border color for form elements and dividers.
	/// </summary>
	public string Border { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the secondary border color for subtle separators and outlines.
	/// </summary>
	public string BorderSecondary { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the border focus color for focused form inputs and interactive elements.
	/// </summary>
	public string BorderFocus { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the border hover color for interactive elements on hover.
	/// </summary>
	public string BorderHover { get; set; } = string.Empty;

	// Text colors
	/// <summary>
	/// Gets or sets the primary text color for main content and headings.
	/// </summary>
	public string TextPrimary { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the secondary text color for supporting content and descriptions.
	/// </summary>
	public string TextSecondary { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the tertiary text color for subtle hints and metadata.
	/// </summary>
	public string TextTertiary { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the disabled text color for inactive or unavailable content.
	/// </summary>
	public string TextDisabled { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the inverse text color for text on dark backgrounds.
	/// </summary>
	public string TextInverse { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the muted text color for placeholders and helper text.
	/// </summary>
	public string TextMuted { get; set; } = string.Empty;

	// Button colors
	/// <summary>
	/// Gets or sets the primary button color for main action buttons.
	/// </summary>
	public string ButtonPrimary { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the primary button hover color for interactive feedback.
	/// </summary>
	public string ButtonPrimaryHover { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the primary button active color for pressed state.
	/// </summary>
	public string ButtonPrimaryActive { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the secondary button color for alternative actions.
	/// </summary>
	public string ButtonSecondary { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the secondary button hover color for interactive feedback.
	/// </summary>
	public string ButtonSecondaryHover { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the secondary button active color for pressed state.
	/// </summary>
	public string ButtonSecondaryActive { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the danger button color for destructive actions.
	/// </summary>
	public string ButtonDanger { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the danger button hover color for destructive action feedback.
	/// </summary>
	public string ButtonDangerHover { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the danger button active color for destructive action pressed state.
	/// </summary>
	public string ButtonDangerActive { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the success button color for positive actions.
	/// </summary>
	public string ButtonSuccess { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the success button hover color for positive action feedback.
	/// </summary>
	public string ButtonSuccessHover { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the success button active color for positive action pressed state.
	/// </summary>
	public string ButtonSuccessActive { get; set; } = string.Empty;

	// Brand colors
	/// <summary>
	/// Gets or sets the primary brand color for the application theme.
	/// </summary>
	public string Primary { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the primary brand hover color for interactive brand elements.
	/// </summary>
	public string PrimaryHover { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the primary brand active color for pressed brand elements.
	/// </summary>
	public string PrimaryActive { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the light variant of the primary brand color.
	/// </summary>
	public string PrimaryLight { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the dark variant of the primary brand color.
	/// </summary>
	public string PrimaryDark { get; set; } = string.Empty;

	// Secondary brand colors
	/// <summary>
	/// Gets or sets the secondary brand color for complementary design elements.
	/// </summary>
	public string Secondary { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the secondary brand hover color for interactive elements.
	/// </summary>
	public string SecondaryHover { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the secondary brand active color for pressed elements.
	/// </summary>
	public string SecondaryActive { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the light variant of the secondary brand color.
	/// </summary>
	public string SecondaryLight { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the dark variant of the secondary brand color.
	/// </summary>
	public string SecondaryDark { get; set; } = string.Empty;

	// Accent colors
	/// <summary>
	/// Gets or sets the accent color for highlights and special emphasis.
	/// </summary>
	public string Accent { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the accent hover color for interactive accent elements.
	/// </summary>
	public string AccentHover { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the light variant of the accent color for subtle emphasis.
	/// </summary>
	public string AccentLight { get; set; } = string.Empty;

	// Semantic state colors
	/// <summary>
	/// Gets or sets the success color for positive states and confirmations.
	/// </summary>
	public string Success { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the light success color for subtle positive indicators.
	/// </summary>
	public string SuccessLight { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the dark success color for prominent positive states.
	/// </summary>
	public string SuccessDark { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the warning color for cautionary states and alerts.
	/// </summary>
	public string Warning { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the light warning color for subtle cautionary indicators.
	/// </summary>
	public string WarningLight { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the dark warning color for prominent warning states.
	/// </summary>
	public string WarningDark { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the error color for negative states and failures.
	/// </summary>
	public string Error { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the light error color for subtle error indicators.
	/// </summary>
	public string ErrorLight { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the dark error color for prominent error states.
	/// </summary>
	public string ErrorDark { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the info color for informational states and messages.
	/// </summary>
	public string Info { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the light info color for subtle informational indicators.
	/// </summary>
	public string InfoLight { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the dark info color for prominent informational states.
	/// </summary>
	public string InfoDark { get; set; } = string.Empty;

	// Interactive elements
	/// <summary>
	/// Gets or sets the link color for hyperlinks and navigation elements.
	/// </summary>
	public string Link { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the link hover color for interactive link feedback.
	/// </summary>
	public string LinkHover { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the visited link color for previously accessed links.
	/// </summary>
	public string LinkVisited { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the active link color for currently pressed links.
	/// </summary>
	public string LinkActive { get; set; } = string.Empty;

	// Selection and highlight
	/// <summary>
	/// Gets or sets the selection color for selected items and text.
	/// </summary>
	public string Selection { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the selection hover color for hovering over selectable items.
	/// </summary>
	public string SelectionHover { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the highlight color for emphasized content and search results.
	/// </summary>
	public string Highlight { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the secondary highlight color for alternative emphasis.
	/// </summary>
	public string HighlightSecondary { get; set; } = string.Empty;
}
