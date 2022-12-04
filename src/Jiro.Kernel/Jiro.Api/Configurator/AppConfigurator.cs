namespace Jiro.Api.Configurator
{
    public class AppConfigurator
    {
        private readonly EventsConfigurator _eventsConfigurator;
        public AppConfigurator(IServiceProvider provider)
        {
            _eventsConfigurator = provider.GetRequiredService<EventsConfigurator>();
        }

        public void ConfigureEvents()
        {
            _eventsConfigurator?.ConfigureLoggingEvents();
        }
    }
}