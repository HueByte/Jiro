using Jiro.Core;
using Jiro.Core.Constants;
using Jiro.Core.IRepositories;
using Jiro.Core.Options;
using Jiro.Core.Services.CommandContext;
using Jiro.Core.Services.CommandHandler;
using Jiro.Core.Services.CommandSystem;
using Jiro.Core.Services.Conversation;
using Jiro.Core.Services.Geolocation;
using Jiro.Core.Services.MessageCache;
using Jiro.Core.Services.Persona;
using Jiro.Core.Services.Semaphore;
using Jiro.Core.Services.Weather;
using Jiro.Infrastructure.Repositories;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using OpenAI;
using OpenAI.Chat;

namespace Jiro.App.Configurator;

public static class Configurator
{
	public static IServiceCollection AddServices (this IServiceCollection services, IConfiguration config)
	{
		services.AddSingleton<ICommandHandlerService, CommandHandlerService>();
		services.AddSingleton<IHelpService, HelpService>();
		services.AddSingleton<EventsConfigurator>();
		services.AddSingleton<ISemaphoreManager, SemaphoreManager>();
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
		services.AddScoped<IConversationCoreService, ConversationCoreService>();
		services.AddScoped<IHistoryOptimizerService, HistoryOptimizerService>();
		services.AddScoped<IMessageManager, MessageManager>();
		services.AddScoped<IPersonaService, PersonaService>();
		services.AddScoped<IPersonalizedConversationService, PersonalizedConversationService>();
		services.AddScoped<ChatClient>((services) =>
		{
			var configManager = services.GetRequiredService<IConfiguration>();
			// string openApiKey = configManager.GetValue<string>(Constants.Environment.OpenAiKey)
			// 	?? services.GetRequiredService<IOptions<BotOptions>>().Value.OpenAIKey;

			string openApiKey = services.GetRequiredService<IOptions<ChatOptions>>().Value.AuthToken;
			return new ChatClient(Jiro.Core.Constants.AI.Gpt4oMiniModel, openApiKey);
		});

		// Repositories
		services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
		services.AddScoped<IMessageRepository, MessageRepository>();

		return services;
	}

	public static IServiceCollection AddOptions (this IServiceCollection services, IConfiguration configuration)
	{
		services.AddOptions();
		services.Configure<ChatOptions>(configuration.GetSection(ChatOptions.Chat));
		services.Configure<LogOptions>(configuration.GetSection(LogOptions.Log));
		services.Configure<JWTOptions>(configuration.GetSection(JWTOptions.JWT));

		return services;
	}

	public static IServiceCollection AddHttpClients (this IServiceCollection services, IConfiguration configuration)
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
