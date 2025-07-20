namespace Jiro.Core.Services.System.Models;

/// <summary>
/// Represents a comprehensive color scheme configuration for theming the application interface.
/// </summary>
public class ColorScheme
{
	/// <summary>
	/// Gets or sets the transparent color value.
	/// </summary>
	public string Transparent { get; set; }

	/// <summary>
	/// Gets or sets the white color value.
	/// </summary>
	public string White { get; set; }

	/// <summary>
	/// Gets or sets the black color value.
	/// </summary>
	public string Black { get; set; }

	// Background colors
	public string Background { get; set; }
	public string BackgroundSecondary { get; set; }
	public string BackgroundTertiary { get; set; }
	public string BackgroundElevated { get; set; }

	// Surface colors
	public string Surface { get; set; }
	public string SurfaceSecondary { get; set; }
	public string SurfaceHover { get; set; }
	public string SurfaceElevated { get; set; }

	// Container colors
	public string Container { get; set; }
	public string ContainerSecondary { get; set; }
	public string ContainerHover { get; set; }
	public string ContainerActive { get; set; }
	public string ContainerElevated { get; set; }

	// Border colors
	public string Border { get; set; }
	public string BorderSecondary { get; set; }
	public string BorderFocus { get; set; }
	public string BorderHover { get; set; }

	// Text colors
	public string TextPrimary { get; set; }
	public string TextSecondary { get; set; }
	public string TextTertiary { get; set; }
	public string TextDisabled { get; set; }
	public string TextInverse { get; set; }
	public string TextMuted { get; set; }

	// Button colors
	public string ButtonPrimary { get; set; }
	public string ButtonPrimaryHover { get; set; }
	public string ButtonPrimaryActive { get; set; }
	public string ButtonSecondary { get; set; }
	public string ButtonSecondaryHover { get; set; }
	public string ButtonSecondaryActive { get; set; }
	public string ButtonDanger { get; set; }
	public string ButtonDangerHover { get; set; }
	public string ButtonDangerActive { get; set; }
	public string ButtonSuccess { get; set; }
	public string ButtonSuccessHover { get; set; }
	public string ButtonSuccessActive { get; set; }

	// Brand colors
	public string Primary { get; set; }
	public string PrimaryHover { get; set; }
	public string PrimaryActive { get; set; }
	public string PrimaryLight { get; set; }
	public string PrimaryDark { get; set; }

	// Secondary brand colors
	public string Secondary { get; set; }
	public string SecondaryHover { get; set; }
	public string SecondaryActive { get; set; }
	public string SecondaryLight { get; set; }
	public string SecondaryDark { get; set; }

	// Accent colors
	public string Accent { get; set; }
	public string AccentHover { get; set; }
	public string AccentLight { get; set; }

	// Semantic state colors
	public string Success { get; set; }
	public string SuccessLight { get; set; }
	public string SuccessDark { get; set; }
	public string Warning { get; set; }
	public string WarningLight { get; set; }
	public string WarningDark { get; set; }
	public string Error { get; set; }
	public string ErrorLight { get; set; }
	public string ErrorDark { get; set; }
	public string Info { get; set; }
	public string InfoLight { get; set; }
	public string InfoDark { get; set; }

	// Interactive elements
	public string Link { get; set; }
	public string LinkHover { get; set; }
	public string LinkVisited { get; set; }
	public string LinkActive { get; set; }

	// Selection and highlight
	public string Selection { get; set; }
	public string SelectionHover { get; set; }
	public string Highlight { get; set; }
	public string HighlightSecondary { get; set; }
}
