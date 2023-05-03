using System.Text;
using Jiro.Api.Authorization;
using Jiro.Api.Authorization.Requirements;
using Jiro.Core.Constants;
using Jiro.Core.Interfaces.IRepositories;
using Jiro.Core.Models;
using Jiro.Core.Options;
using Jiro.Core.Services.Auth;
using Jiro.Core.Services.CommandHandler;
using Jiro.Core.Services.CommandSystem;
using Jiro.Core.Services.GPTService;
using Jiro.Core.Services.Instance;
using Jiro.Core.Services.WeatherService;
using Jiro.Core.Services.Whitelist;
using Jiro.Infrastructure;
using Jiro.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

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
            services.AddSingleton<ICurrentInstanceService, CurrentInstanceService>();

            services.AddScoped<IWeatherService, WeatherService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IJWTService, JWTService>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            services.AddScoped<IWhitelistService, WhitelistService>();
            services.AddScoped<IJiroInstanceService, JiroInstanceService>();

            // repositories
            services.AddScoped<IWhitelistRepository, WhitelistRepository>();
            services.AddScoped<IJiroInstanceRepository, JiroInstanceRepository>();

            return services;
        }

        public static IServiceCollection AddOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<GptOptions>(configuration.GetSection(GptOptions.Gpt));
            services.Configure<ChatGptOptions>(configuration.GetSection($"{GptOptions.Gpt}:{ChatGptOptions.ChatGpt}"));
            services.Configure<SingleGptOptions>(configuration.GetSection($"{GptOptions.Gpt}:{SingleGptOptions.SingleGpt}"));
            services.Configure<LogOptions>(configuration.GetSection(LogOptions.Log));
            services.Configure<JWTOptions>(configuration.GetSection(JWTOptions.JWT));

            return services;
        }

        public static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtOptions = configuration.GetSection(JWTOptions.JWT).Get<JWTOptions>();
            var isWhitelistEnabled = configuration.GetSection("Whitelist").Get<bool>();

            services.AddIdentity<AppUser, AppRole>()
                .AddEntityFrameworkStores<JiroContext>()
                .AddDefaultTokenProviders();

            services.Configure<IdentityOptions>(options =>
            {
                // password options
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 6;
                options.Password.RequiredUniqueChars = 1;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

                // user settings
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                options.User.RequireUniqueEmail = true;
            });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    RequireExpirationTime = true,
                    ValidIssuer = jwtOptions?.Issuer,
                    ValidAudience = jwtOptions?.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions?.Secret!)),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Cookies["X-Access-Token"];
                        return Task.CompletedTask;
                    },
                };
            });

            services.AddScoped<IAuthorizationHandler, WhitelistAuthorizationHandler>();

            services.AddAuthorization(options =>
            {
                options.AddPolicy(Jiro.Core.Constants.Policies.WHITE_LIST, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.Requirements.Add(new WhitelistRequirement(isWhitelistEnabled));
                });
            });

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