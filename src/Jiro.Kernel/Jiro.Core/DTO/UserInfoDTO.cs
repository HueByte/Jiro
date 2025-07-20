namespace Jiro.Core.DTO;

/// <summary>
/// Data transfer object containing user information for display and management purposes.
/// </summary>
public class UserInfoDTO
{
	/// <summary>
	/// Gets or sets the user's username.
	/// </summary>
	public string Username { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the user's email address.
	/// </summary>
	public string Email { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the unique identifier of the user.
	/// </summary>
	public string Id { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the array of roles assigned to the user.
	/// </summary>
	public string[]? Roles
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the date and time when the user account was created.
	/// </summary>
	public DateTime AccountCreatedDate
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets a value indicating whether the user is whitelisted for access.
	/// </summary>
	public bool IsWhitelisted
	{
		get; set;
	}
}
