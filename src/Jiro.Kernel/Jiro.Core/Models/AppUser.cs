using Microsoft.AspNetCore.Identity;

namespace Jiro.Core.Models;

/// <summary>
/// Represents an application user with extended properties beyond the base ASP.NET Core Identity user.
/// </summary>
public class AppUser : IdentityUser
{
	/// <summary>
	/// Gets or sets the collection of user roles associated with this user.
	/// </summary>
	public virtual ICollection<AppUserRole> UserRoles { get; set; } = default!;

	/// <summary>
	/// Gets or sets the collection of refresh tokens associated with this user for authentication purposes.
	/// </summary>
	public virtual List<RefreshToken>? RefreshTokens { get; set; }

	/// <summary>
	/// Gets or sets the date and time when the user account was created.
	/// </summary>
	public DateTime AccountCreatedDate { get; set; }
}
