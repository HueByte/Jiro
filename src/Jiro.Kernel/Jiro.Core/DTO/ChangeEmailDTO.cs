namespace Jiro.Core.DTO;

/// <summary>
/// Data transfer object for email change requests, requiring the new email and current password for verification.
/// </summary>
public class ChangeEmailDTO
{
	/// <summary>
	/// Gets or sets the new email address to change to.
	/// </summary>
	public string NewEmail { get; set; } = default!;

	/// <summary>
	/// Gets or sets the current password for verification of the change request.
	/// </summary>
	public string Password { get; set; } = default!;
}
