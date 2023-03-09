using System.Reflection;
using Jiro.Core.Commands.GPT;
using Microsoft.Extensions.DependencyInjection;

namespace Jiro.Core.Base;

public static class CommandRegistrator
{
    public static IServiceCollection RegisterCommands(this IServiceCollection services)
    {
        var assemblies = ReflectionUtilities.GetDomainAssemblies();
        if (assemblies is null) throw new Exception("Assemblies is null, something went wrong");

        // types that contain CommandContainer attribute
        var commandModules = ReflectionUtilities.GetCommandModules(assemblies);
        if (commandModules is null) throw new Exception("Command modules is null, something went wrong");

        List<CommandModuleInfo> commandModulesInfos = new();

        foreach (var container in commandModules)
        {
            CommandModuleInfo commandModuleInfo = new();
            List<CommandInfo> commands = new();

            var preCommands = ReflectionUtilities.GetPotentialCommands(container);
            foreach (var methodInfo in preCommands)
            {
                var commandInfo = ReflectionUtilities.BuildCommandFromMethodInfo<ICommandBase, Task>(methodInfo);
                if (commandInfo is not null) commands.Add(commandInfo);
            }

            commandModuleInfo.SetCommands(commands);
            commandModuleInfo.SetName(container.GetCustomAttribute<CommandModuleAttribute>()?.ModuleName ?? "");

            services.AddScoped(container);
            commandModulesInfos.Add(commandModuleInfo);
        };

        CommandsContext globalContainer = new();

        // add default command 
        globalContainer.SetDefaultCommand(nameof(GPTCommand.Chat).ToLower());
        globalContainer.AddModules(commandModulesInfos);
        globalContainer.AddCommands(commandModulesInfos
            .SelectMany(x => x.Commands.Select(e => e.Value))
            .ToList());

        services.AddSingleton(globalContainer);

        return services;
    }
}
