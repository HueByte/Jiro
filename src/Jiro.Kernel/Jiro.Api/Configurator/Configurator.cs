using Jiro.Core.Base;
using Jiro.Core.Constants;
using Jiro.Core.Models;
using Jiro.Core.Options;
using Jiro.Core.Services.Auth;
using Jiro.Core.Services.CommandHandler;
using Jiro.Core.Services.CommandSystem;
using Jiro.Core.Services.GPTService;
using Jiro.Core.Services.WeatherService;
using Jiro.Infrastructure;
using Microsoft.AspNetCore.Identity;
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

            // services
            services.AddSingleton<IChatGPTStorageService, ChatGPTStorageService>();
            services.AddSingleton<ITokenizerService, TokenizerService>();
            services.AddSingleton<ICommandHandlerService, CommandHandlerService>();
            services.AddSingleton<IHelpService, HelpService>();
            services.AddSingleton<EventsConfigurator>();

            services.AddScoped<IWeatherService, WeatherService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IJWTService, JWTService>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();

            // repositories


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

        public static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddIdentity<AppUser, AppRole>()
                .AddEntityFrameworkStores<JiroContext>()
                .AddDefaultTokenProviders();

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

            services.AddHttpClient(HttpClients.JIRO);

            return services;
        }
    }
}