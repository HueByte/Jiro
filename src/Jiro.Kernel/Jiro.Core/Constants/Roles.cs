namespace Jiro.Core.Constants;

/// <summary>
/// Contains user role constants used for authorization and access control.
/// </summary>
public class Roles
{
	/// <summary>
	/// The administrator role with full system access privileges.
	/// </summary>
	public const string ADMIN = "admin";

	/// <summary>
	/// The standard user role with basic access privileges.
	/// </summary>
	public const string USER = "user";

	/// <summary>
	/// The server role used for system-to-system communication.
	/// </summary>
	public const string SERVER = "server";
}
