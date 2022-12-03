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


            List<CommandModule> commandModules = new();
            List<CommandInfo> commandInfos = new();
            foreach (var container in commandContainers)
            {
                CommandModule commandModule = new();
                var commands = GetCommands(container);
                commandModule.SetCommands(commands);
                commandModule.SetName(container.GetCustomAttribute<CommandContainerAttribute>()?.ContainerName ?? "");

                services.AddScoped(container);
                commandModules.Add(commandModule);
                commandInfos.AddRange(commands);
            };

            CommandsContainer globalContainer = new();
            globalContainer.AddModules(commandModules);
            globalContainer.AddCommands(commandInfos);
            globalContainer.SetDefaultCommand("chat");
            services.AddSingleton(globalContainer);

            return services;
        }

        private static List<CommandInfo> GetCommands(Type type)
        {
            return type
                .GetMethods()
                .Where(method => method.GetCustomAttributes(typeof(CommandAttribute), false).Length > 0)
                .Select(method => new CommandInfo(
                    method.GetCustomAttribute<CommandAttribute>()?.CommandName.ToLower() ?? "",
                    method.GetCustomAttribute<AsyncStateMachineAttribute>() is not null,
                    method.DeclaringType!,
                    method
                ))
                .ToList();
        }
    }
}