using Jiro.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Jiro.Infrastructure;

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

        builder.Entity<ChatSession>()
                .HasMany(e => e.Messages)
                .WithOne(e => e.ChatSession)
                .HasForeignKey(e => e.ChatSessionId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Message>()
                .HasOne(e => e.ChatSession)
                .WithMany(e => e.Messages)
                .HasForeignKey(e => e.ChatSessionId)
                .IsRequired();
    }

    public DbSet<ChatSession> ChatSessions { get; set; } = default!;
    public DbSet<Message> Messages { get; set; } = default!;
}