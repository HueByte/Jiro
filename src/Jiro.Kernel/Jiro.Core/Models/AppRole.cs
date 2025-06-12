using Microsoft.AspNetCore.Identity;

namespace Jiro.Core.Models;

public class AppRole : IdentityRole
{
    public virtual ICollection<AppUserRole>? UserRoles { get; set; }
}
