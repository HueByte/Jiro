namespace Jiro.Core.DTO;

/// <summary>
/// Data transfer object containing a user identifier for operations that require only the user ID.
/// </summary>
public class UserIdDTO
{
	/// <summary>
	/// Gets or sets the unique identifier of the user.
	/// </summary>
	public string UserId { get; set; } = default!;
}
