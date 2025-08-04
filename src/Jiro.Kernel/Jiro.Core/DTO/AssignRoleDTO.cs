namespace Jiro.Core.DTO;

/// <summary>
/// Data transfer object for role assignment requests to assign a specific role to a user.
/// </summary>
public class AssignRoleDTO
{
	/// <summary>
	/// Gets or sets the unique identifier of the user to assign the role to.
	/// </summary>
	public string UserId { get; set; } = default!;

	/// <summary>
	/// Gets or sets the name of the role to assign to the user.
	/// </summary>
	public string Role { get; set; } = default!;
}
