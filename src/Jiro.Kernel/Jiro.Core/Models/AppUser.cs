using Microsoft.AspNetCore.Identity;

namespace Jiro.Core.Models;

public class AppUser : IdentityUser
{
    public virtual ICollection<AppUserRole> UserRoles { get; set; } = default!;
    public virtual List<RefreshToken>? RefreshTokens { get; set; }
    public DateTime AccountCreatedDate { get; set; }
}
