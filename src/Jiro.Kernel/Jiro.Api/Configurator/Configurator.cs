using Jiro.Core.Base;
using Jiro.Core.Constants;
using Jiro.Core.Options;
using Jiro.Core.Services.CommandHandler;
using Jiro.Core.Services.CommandSystem;
using Jiro.Core.Services.GPTService;
using Jiro.Core.Services.WeatherService;
using Microsoft.Extensions.Options;

namespace Jiro.Api.Configurator
{
    public static class Configurator
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration config)
        {
            GptOptions? gptOptions = config.GetSection(GptOptions.Gpt).Get<GptOptions>();

            if (gptOptions is { Enable: true, AuthToken: not null and not "" })
            {
                if (gptOptions.UseChatGpt) services.AddScoped<IChatService, ChatGPTService>();
                else services.AddScoped<IChatService, GPTService>();
            }
            else
            {
                services.AddScoped<IChatService, DisabledGptService>();
            }

            services.AddSingleton<IChatGPTStorageService, ChatGPTStorageService>();
            services.AddSingleton<ITokenizerService, TokenizerService>();


            services.AddScoped<IWeatherService, WeatherService>();

            services.AddSingleton<ICommandHandlerService, CommandHandlerService>();
            services.AddSingleton<IHelpService, HelpService>();
            services.AddSingleton<EventsConfigurator>();

            return services;
        }

        public static IServiceCollection AddOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<GptOptions>(configuration.GetSection(GptOptions.Gpt));
            services.Configure<ChatGptOptions>(configuration.GetSection($"{GptOptions.Gpt}:{ChatGptOptions.ChatGpt}"));
            services.Configure<SingleGptOptions>(configuration.GetSection($"{GptOptions.Gpt}:{SingleGptOptions.SingleGpt}"));
            services.Configure<LogOptions>(configuration.GetSection(LogOptions.Log));

            return services;
        }

        public static IServiceCollection RegisterCommandModules(this IServiceCollection services)
        {
            services.RegisterCommands();

            return services;
        }

        public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient(HttpClients.GPT_CLIENT, (provider, httpClient) =>
            {
                var gptOptions = provider.GetRequiredService<IOptions<GptOptions>>().Value;

                httpClient.BaseAddress = new Uri(gptOptions.BaseUrl);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {gptOptions.AuthToken}");

                if (!string.IsNullOrEmpty(gptOptions.Organization))
                    httpClient.DefaultRequestHeaders.Add("OpenAI-Organization", gptOptions.Organization);
            });

            services.AddHttpClient(HttpClients.WEATHER_CLIENT, httpClient =>
            {
                httpClient.BaseAddress = new Uri("https://api.open-meteo.com/v1/");
            });


            services.AddHttpClient(HttpClients.GEOLOCATION_CLIENT, httpClient =>
            {
                httpClient.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
                httpClient.DefaultRequestHeaders.Add("User-Agent", "JiroBot");
            });

            services.AddHttpClient(HttpClients.CHAT_GPT_CLIENT, (provider, httpClient) =>
            {
                var gptOptions = provider.GetRequiredService<IOptions<GptOptions>>().Value;

                httpClient.BaseAddress = new Uri(gptOptions.BaseUrl);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {gptOptions.AuthToken}");

                if (!string.IsNullOrEmpty(gptOptions.Organization))
                    httpClient.DefaultRequestHeaders.Add("OpenAI-Organization", gptOptions.Organization);
            });

            services.AddHttpClient(HttpClients.TOKENIZER, (provider, httpClient) =>
            {
                var tokenizerUrl = provider.GetRequiredService<IConfiguration>()
                    .GetValue<string>("TokenizerUrl");

                httpClient.BaseAddress = new Uri(tokenizerUrl ?? "http://localhost:8000");
            });

            return services;
        }
    }
}