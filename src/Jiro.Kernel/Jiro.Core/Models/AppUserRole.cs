using Microsoft.AspNetCore.Identity;

namespace Jiro.Core.Models;

/// <summary>
/// Represents the many-to-many relationship between users and roles in the application.
/// </summary>
public class AppUserRole : IdentityUserRole<string>
{
	/// <summary>
	/// Gets or sets the user associated with this user-role relationship.
	/// </summary>
	public virtual AppUser User { get; set; } = default!;

	/// <summary>
	/// Gets or sets the role associated with this user-role relationship.
	/// </summary>
	public virtual AppRole Role { get; set; } = default!;
}
