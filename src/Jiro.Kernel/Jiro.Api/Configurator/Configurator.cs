using System.Globalization;
using Jiro.Core.Commands.Base;
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
            services.AddScoped<ICommandHandlerService, CommandHandlerService>();

            return services;
        }

        public static IServiceCollection RegisterCommandModules(this IServiceCollection services)
        {
            services.RegisterCommands();

            return services;
        }

        public static IServiceCollection AddHttpClients(this IServiceCollection services)
        {
            return services;
        }
    }
}