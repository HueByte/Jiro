namespace Jiro.Api.Configurator
{
    public class AppConfigurator
    {
        private readonly EventsConfigurator _eventsConfigurator;
        public AppConfigurator(IServiceProvider provider)
        {
            _eventsConfigurator = provider.GetRequiredService<EventsConfigurator>();
        }

        public AppConfigurator ConfigureEvents()
        {
            _eventsConfigurator?.ConfigureLoggingEvents();

            return this;
        }
    }
}