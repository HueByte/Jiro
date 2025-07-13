using Jiro.Core.Models;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Jiro.Infrastructure;

/// <summary>
/// Entity Framework database context for the Jiro application, extending IdentityDbContext for user authentication
/// and providing access to chat sessions, messages, and refresh tokens.
/// </summary>
public class JiroContext : IdentityDbContext<AppUser, AppRole, string,
	IdentityUserClaim<string>, AppUserRole, IdentityUserLogin<string>,
	IdentityRoleClaim<string>, IdentityUserToken<string>>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="JiroContext"/> class with default configuration.
	/// </summary>
	public JiroContext()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="JiroContext"/> class with the specified options.
	/// </summary>
	/// <param name="options">The database context options for configuring the context.</param>
	public JiroContext(DbContextOptions<JiroContext> options) : base(options) { }

	/// <summary>
	/// Configures the database context options if not already configured through dependency injection.
	/// </summary>
	/// <param name="optionsBuilder">The options builder used to configure the context.</param>
	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
	}

	/// <summary>
	/// Configures the entity relationships and database model for the Jiro application,
	/// including user-role relationships for Identity framework.
	/// </summary>
	/// <param name="builder">The model builder used to configure entity relationships.</param>
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

		// Configure ChatSession -> Messages relationship
		builder.Entity<ChatSession>()
			   .HasMany(e => e.Messages)
			   .WithOne()
			   .HasForeignKey(m => m.SessionId)
			   .IsRequired();

		// Configure Message entity
		builder.Entity<Message>()
			   .Property(e => e.Id)
			   .ValueGeneratedNever(); // We handle ID generation ourselves
	}

	/// <summary>
	/// Gets or sets the database set for chat sessions in the application.
	/// </summary>
	public DbSet<ChatSession> ChatSessions { get; set; } = default!;

	/// <summary>
	/// Gets or sets the database set for messages exchanged in chat sessions.
	/// </summary>
	public DbSet<Message> Messages { get; set; } = default!;

	/// <summary>
	/// Gets or sets the database set for refresh tokens used in authentication.
	/// </summary>
	public DbSet<RefreshToken> RefreshTokens { get; set; } = default!;
}
