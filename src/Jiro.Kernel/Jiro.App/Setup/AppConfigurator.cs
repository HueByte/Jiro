using Jiro.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jiro.App.Setup;

/// <summary>
/// Provides configuration functionality for the Jiro application, including event configuration and database migration.
/// </summary>
public class AppConfigurator
{
	private readonly IHost _app;
	private readonly EventsConfigurator _eventsConfigurator;

	/// <summary>
	/// Initializes a new instance of the <see cref="AppConfigurator"/> class.
	/// </summary>
	/// <param name="app">The host application instance used for dependency injection and service resolution.</param>
	public AppConfigurator(IHost app)
	{
		_app = app;
		_eventsConfigurator = app.Services.GetRequiredService<EventsConfigurator>();
	}

	/// <summary>
	/// Configures the application events, including logging events setup.
	/// </summary>
	/// <returns>The current <see cref="AppConfigurator"/> instance for method chaining.</returns>
	public AppConfigurator ConfigureEvents()
	{
		_eventsConfigurator?.ConfigureLoggingEvents();

		return this;
	}

	/// <summary>
	/// Executes database migrations for the Jiro application context.
	/// </summary>
	/// <returns>The current <see cref="AppConfigurator"/> instance for method chaining.</returns>
	public AppConfigurator Migrate()
	{
		using var scope = _app.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<JiroContext>();

		// Ensure database directory exists for SQLite
		var connection = context.Database.GetDbConnection();
		var dataSource = connection.DataSource;

		// For Sqlite, DataSource is the file path
		if (!string.IsNullOrWhiteSpace(dataSource))
		{
			var dbDirectory = Path.GetDirectoryName(dataSource);
			if (!string.IsNullOrWhiteSpace(dbDirectory) && !Directory.Exists(dbDirectory))
			{
				Directory.CreateDirectory(dbDirectory);
			}
		}

		context.Database.MigrateAsync().GetAwaiter().GetResult();

		return this;
	}

}
