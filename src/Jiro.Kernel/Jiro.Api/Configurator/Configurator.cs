using Jiro.Core.Base;
using Jiro.Core.Interfaces.IServices;
using Jiro.Core.Services.CommandHandler;
using Jiro.Core.Services.GPTService;

namespace Jiro.Api.Configurator
{
    public static class Configurator
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddScoped<IGPTService, GPTService>();

            services.AddSingleton<ICommandHandlerService, CommandHandlerService>();
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
            services.AddHttpClient("GPT", httpclient =>
            {
                var baseUrl = configuration.GetSection("GPT:BaseUrl").Get<string>();
                var authToken = configuration.GetSection("GPT:Token").Get<string>();
                var organization = configuration.GetSection("GPT:Organization").Get<string>();

                httpclient.BaseAddress = new Uri(baseUrl!);
                httpclient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

                if (!string.IsNullOrEmpty(organization))
                    httpclient.DefaultRequestHeaders.Add("OpenAI-Organization", organization);
            });

            return services;
        }
    }
}