using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jiro.Infrastructure;

/// <summary>
/// Provides extension methods for configuring database contexts with different database providers.
/// </summary>
public static class Extensions
{
	/// <summary>
	/// Adds and configures the Jiro database context to use MySQL as the database provider.
	/// </summary>
	/// <param name="services">The service collection to add the database context to.</param>
	/// <param name="conn">The MySQL connection string.</param>
	/// <returns>The service collection with the configured MySQL database context.</returns>
	public static IServiceCollection AddJiroMySQLContext(this IServiceCollection services, string conn)
	{
		services.AddDbContext<JiroContext>(options =>
		{
			options.UseMySql(ServerVersion.AutoDetect(conn),
				x => x.MigrationsAssembly(typeof(JiroContext).Assembly.GetName().Name));
		});

		return services;
	}

	/// <summary>
	/// Adds and configures the Jiro database context to use SQLite as the database provider.
	/// </summary>
	/// <param name="services">The service collection to add the database context to.</param>
	/// <param name="conn">The SQLite database file path.</param>
	/// <returns>The service collection with the configured SQLite database context.</returns>
	public static IServiceCollection AddJiroSQLiteContext(this IServiceCollection services, string conn)
	{
		services.AddDbContext<JiroContext>(options =>
		{
			options.UseSqlite($"Data Source={conn}",
				x => x.MigrationsAssembly(typeof(JiroContext).Assembly.GetName().Name));
		});

		return services;
	}
}
