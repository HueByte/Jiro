using Jiro.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Jiro.Infrastructure
{
    public class JiroContext : IdentityDbContext<AppUser, AppRole, string,
        IdentityUserClaim<string>, AppUserRole, IdentityUserLogin<string>,
        IdentityRoleClaim<string>, IdentityUserToken<string>>
    {
        public JiroContext() { }
        public JiroContext(DbContextOptions<JiroContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<AppUser>()
                   .HasMany(e => e.UserRoles)
                   .WithOne(e => e.User)
                   .HasForeignKey(e => e.UserId)
                   .IsRequired();

            builder.Entity<AppRole>()
                   .HasMany(e => e.UserRoles)
                   .WithOne(e => e.Role)
                   .HasForeignKey(e => e.RoleId)
                   .IsRequired();
        }

        public DbSet<WhiteListEntry> WhiteListEntries => Set<WhiteListEntry>();
        public DbSet<JiroInstance> JiroInstances => Set<JiroInstance>();
    }
}