using Microsoft.AspNetCore.Identity;

namespace Jiro.Core.Models;

/// <summary>
/// Represents an application role with extended properties beyond the base ASP.NET Core Identity role.
/// </summary>
public class AppRole : IdentityRole
{
	/// <summary>
	/// Gets or sets the collection of user-role associations for this role.
	/// </summary>
	public virtual ICollection<AppUserRole>? UserRoles { get; set; }
}
