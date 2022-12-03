using System.Reflection;
using System.Runtime.CompilerServices;
using Jiro.Core.Attributes;
using Jiro.Core.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Jiro.Core.Commands.Base
{
    public static class CommandRegistrator
    {
        public static IServiceCollection RegisterCommands(this IServiceCollection services)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var commandContainers = assemblies
                .SelectMany(asm => asm.GetTypes()
                    .Where(type =>
                        !type.IsInterface
                        && type.GetCustomAttributes(typeof(CommandContainerAttribute), false).Length > 0
                ))
                .ToList();

            var commandMethods = commandContainers
                .SelectMany(container =>
                    container.GetMethods()
                        .Where(method => method.GetCustomAttributes(typeof(CommandAttribute), false).Length > 0))
                .ToList();

            var commands = commandMethods.Select(method => new CommandInfo
            (
                method.GetCustomAttribute<CommandAttribute>()?.CommandName.ToLower() ?? "",
                method.GetCustomAttribute<AsyncStateMachineAttribute>() is not null,
                method.DeclaringType!,
                method
            ));

            foreach (var commandContainer in commandContainers)
            {
                Console.WriteLine($"Registering {commandContainer} command container");
                services.AddScoped(commandContainer);
            }

            foreach (var command in commands)
                Console.WriteLine($"Created command {command.Name} | {command.IsAsync}");

            CommandModule commandModule = new();
            commandModule.SetDefaultCommand("chat");
            commandModule.AddCommands(commands.ToList());

            services.AddSingleton(commandModule);

            return services;
        }
    }
}