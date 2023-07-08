using Microsoft.AspNetCore.Identity;

namespace Jiro.Core.Models;

public class AppUserRole : IdentityUserRole<string>
{
    public virtual AppUser User { get; set; } = default!;
    public virtual AppRole Role { get; set; } = default!;
}