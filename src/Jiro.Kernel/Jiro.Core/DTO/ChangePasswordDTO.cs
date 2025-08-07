namespace Jiro.Core.DTO;

/// <summary>
/// Data transfer object for password change requests, requiring both current and new passwords.
/// </summary>
public class ChangePasswordDTO
{
	/// <summary>
	/// Gets or sets the current password for verification of the change request.
	/// </summary>
	public string CurrentPassword { get; set; } = default!;

	/// <summary>
	/// Gets or sets the new password to change to.
	/// </summary>
	public string NewPassword { get; set; } = default!;
}
