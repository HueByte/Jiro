using Jiro.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jiro.App.Configurator;

public class AppConfigurator
{
	private readonly IHost _app;
	private readonly EventsConfigurator _eventsConfigurator;
	public AppConfigurator(IHost app)
	{
		_app = app;
		_eventsConfigurator = app.Services.GetRequiredService<EventsConfigurator>();
	}

	public AppConfigurator ConfigureEvents()
	{
		_eventsConfigurator?.ConfigureLoggingEvents();

		return this;
	}

	public AppConfigurator Migrate()
	{
		using var scope = _app.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<JiroContext>();

		context.Database.Migrate();

		return this;
	}
}
