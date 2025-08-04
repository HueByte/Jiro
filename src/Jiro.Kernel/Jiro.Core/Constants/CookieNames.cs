namespace Jiro.Core.Constants;

/// <summary>
/// Contains cookie name constants used for authentication and session management.
/// </summary>
public class CookieNames
{
	/// <summary>
	/// The cookie name for storing refresh tokens.
	/// </summary>
	public const string REFRESH_TOKEN = "X-Refresh-Token";

	/// <summary>
	/// The cookie name for storing access tokens.
	/// </summary>
	public const string ACCESS_TOKEN = "X-Access-Token";
}
