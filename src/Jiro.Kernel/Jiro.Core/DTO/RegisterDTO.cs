namespace Jiro.Core.DTO;

/// <summary>
/// Data transfer object for user registration requests.
/// </summary>
public class RegisterDTO
{
	/// <summary>
	/// Gets or sets the desired username for the new account.
	/// </summary>
	public string Username { get; set; } = default!;

	/// <summary>
	/// Gets or sets the email address for the new account.
	/// </summary>
	public string Email { get; set; } = default!;

	/// <summary>
	/// Gets or sets the password for the new account.
	/// </summary>
	public string Password { get; set; } = default!;
}
