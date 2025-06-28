using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jiro.Infrastructure;

public static class Extensions
{
	public static IServiceCollection AddJiroMySQLContext(this IServiceCollection services, string conn)
	{
		services.AddDbContext<JiroContext>(options =>
		{
			options.UseMySql(ServerVersion.AutoDetect(conn),
				x => x.MigrationsAssembly(typeof(JiroContext).Assembly.GetName().Name));
		});

		return services;
	}

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
