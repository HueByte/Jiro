using Microsoft.AspNetCore.Identity;

namespace Jiro.Core.Models
{
    public class AppUser : IdentityUser
    {
        public virtual ICollection<AppUserRole> UserRoles { get; set; } = default!;
        public DateTime AccountCreatedDate { get; set; }
    }
}