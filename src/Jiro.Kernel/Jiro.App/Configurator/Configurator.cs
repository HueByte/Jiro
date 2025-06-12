using Jiro.Core;
using Jiro.Core.Constants;
using Jiro.Core.Interfaces.IServices;
using Jiro.Core.IRepositories;
using Jiro.Core.Options;
using Jiro.Core.Services.Chat;
using Jiro.Core.Services.CommandHandler;
using Jiro.Core.Services.CommandSystem;
using Jiro.Core.Services.CurrentUser;
using Jiro.Core.Services.Geolocation;
using Jiro.Core.Services.Weather;
using Jiro.Infrastructure.Repositories;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using OpenAI;

namespace Jiro.App.Configurator;

public static class Configurator
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration config)
    {
        ChatOptions? chatOptions = config.GetSection(ChatOptions.Chat).Get<ChatOptions>();
        if (chatOptions is { Enabled: true, AuthToken: not null and not "" })
        {
            services.AddScoped<IChatService, ChatService>();

        }
        else
        {
            services.AddScoped<IChatService, DisabledChatService>();
        }

        services.AddSingleton<ICommandHandlerService, CommandHandlerService>();
        services.AddSingleton<IHelpService, HelpService>();
        services.AddSingleton<EventsConfigurator>();
        services.AddSingleton<OpenAIClient>((factoryServices) =>
        {
            var apiKey = config.GetSection(ChatOptions.Chat).Get<ChatOptions>()?.AuthToken;
            if (apiKey is null)
            {
                throw new JiroException("API Key for chat not found");
            }

            return new OpenAIClient(apiKey);
        });

        services.AddScoped<IWeatherService, WeatherService>();
        services.AddScoped<ICommandContext, CommandContext>();
        services.AddScoped<IGeolocationService, GeolocationService>();
        services.AddScoped<IChatStorageService, ChatStorageService>();

        // Repositories
        services.AddScoped<IChatSessionRepository, ChatSessionRepository>();

        return services;
    }

    public static IServiceCollection AddOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions();
        services.Configure<ChatOptions>(configuration.GetSection(ChatOptions.Chat));
        services.Configure<LogOptions>(configuration.GetSection(LogOptions.Log));
        services.Configure<JWTOptions>(configuration.GetSection(JWTOptions.JWT));

        return services;
    }

    public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient(HttpClients.WEATHER_CLIENT, httpClient =>
        {
            httpClient.BaseAddress = new Uri("https://api.open-meteo.com/v1/");
        });

        services.AddHttpClient(HttpClients.GEOLOCATION_CLIENT, httpClient =>
        {
            httpClient.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "JiroBot");
        });

        services.AddHttpClient(HttpClients.JIRO);



        return services;
    }
}
