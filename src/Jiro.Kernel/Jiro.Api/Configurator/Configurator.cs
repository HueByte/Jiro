using Jiro.Core.Base;
using Jiro.Core.Constants;
using Jiro.Core.Interfaces.IServices;
using Jiro.Core.Services.CommandHandler;
using Jiro.Core.Services.CommandSystem;
using Jiro.Core.Services.GPTService;
using Jiro.Core.Services.WeatherService;

namespace Jiro.Api.Configurator
{
    public static class Configurator
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddScoped<IGPTService, GPTService>();
            services.AddScoped<IWeatherService, WeatherService>();

            services.AddSingleton<ICommandHandlerService, CommandHandlerService>();
            services.AddSingleton<IHelpService, HelpService>();
            services.AddSingleton<EventsConfigurator>();

            return services;
        }

        public static IServiceCollection RegisterCommandModules(this IServiceCollection services)
        {
            services.RegisterCommands();

            return services;
        }

        public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient(HttpClientNames.GPT_CLIENT, httpclient =>
            {
                var baseUrl = configuration.GetSection("GPT:BaseUrl").Get<string>();
                var authToken = configuration.GetSection("GPT:Token").Get<string>();
                var organization = configuration.GetSection("GPT:Organization").Get<string>();

                httpclient.BaseAddress = new Uri(baseUrl!);
                httpclient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

                if (!string.IsNullOrEmpty(organization))
                    httpclient.DefaultRequestHeaders.Add("OpenAI-Organization", organization);
            });

            services.AddHttpClient(HttpClientNames.WEATHER_CLIENT, httpClient =>
            {
                httpClient.BaseAddress = new Uri("https://api.open-meteo.com/v1/");
            });


            services.AddHttpClient(HttpClientNames.GEOLOCATION_CLIENT, httpClient =>
            {
                httpClient.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
                httpClient.DefaultRequestHeaders.Add("User-Agent", "JiroBot");
            });

            return services;
        }
    }
}