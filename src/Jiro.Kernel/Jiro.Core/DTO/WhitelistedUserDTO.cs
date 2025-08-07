namespace Jiro.Core.DTO;

/// <summary>
/// Data transfer object representing a user with whitelist status information.
/// </summary>
public class WhitelistedUserDTO
{
	/// <summary>
	/// Gets or sets the unique identifier of the user.
	/// </summary>
	public string Id { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the email address of the user.
	/// </summary>
	public string Email { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the username of the user.
	/// </summary>
	public string Username { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets a value indicating whether the user is whitelisted for access.
	/// </summary>
	public bool IsWhitelisted
	{
		get; set;
	}
}
