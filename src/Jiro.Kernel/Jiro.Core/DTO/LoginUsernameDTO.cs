namespace Jiro.Core.DTO;

/// <summary>
/// Data transfer object for user login requests using username and password.
/// </summary>
public class LoginUsernameDTO
{
	/// <summary>
	/// Gets or sets the username for authentication.
	/// </summary>
	public string Username { get; set; } = default!;

	/// <summary>
	/// Gets or sets the password for authentication.
	/// </summary>
	public string Password { get; set; } = default!;
}
