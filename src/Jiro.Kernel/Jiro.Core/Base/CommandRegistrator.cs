using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Jiro.Core.Attributes;
using Jiro.Core.Commands.GPT;
using Microsoft.Extensions.DependencyInjection;

namespace Jiro.Core.Base;

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
        globalContainer.SetDefaultCommand(nameof(GPTCommand.Chat).ToLower());
        services.AddSingleton(globalContainer);

        return services;
    }

    private static List<CommandInfo> GetCommands(Type type)
    {
        List<CommandInfo> commandInfos = new();
        var methodInfos = type
            .GetMethods()
            .Where(method => method.GetCustomAttributes(typeof(CommandAttribute), false).Length > 0)
            .ToList();

        var createMethodInvoker = typeof(CommandRegistrator)
            ?.GetMethod("CreateMethodInvoker");

        foreach (var methodInfo in methodInfos)
        {
            if (methodInfo is null) continue;

            var delcaringType = methodInfo?.DeclaringType;

            if (delcaringType is null) continue;

            var compiledLambda = CreateMethodInvoker(methodInfo!);

            CommandInfo commandInfo = new(
                methodInfo!.GetCustomAttribute<CommandAttribute>()?.CommandName.ToLower() ?? "",
                methodInfo!.GetCustomAttribute<AsyncStateMachineAttribute>() is not null,
                delcaringType,
                compiledLambda as Func<CommandBase, object[], Task>
            );

            commandInfos.Add(commandInfo);
        }

        return commandInfos;
    }

    public static Func<CommandBase, object[], Task> CreateMethodInvoker(MethodInfo methodInfo)
    {
        var parameters = methodInfo.GetParameters();
        var paramsExp = new Expression[parameters.Length];

        var instanceExp = Expression.Parameter(typeof(CommandBase), "instance");
        var argsExp = Expression.Parameter(typeof(object[]), "args");

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];

            var indexExp = Expression.Constant(i);
            var accessExp = Expression.ArrayIndex(argsExp, indexExp);
            paramsExp[i] = Expression.Convert(accessExp, parameter.ParameterType);
        }

        var callExp = Expression.Call(Expression.Convert(instanceExp, methodInfo.ReflectedType!), methodInfo, paramsExp);
        var finalExp = Expression.Convert(callExp, typeof(Task));
        var lambda = Expression.Lambda<Func<CommandBase, object[], Task>>(finalExp, instanceExp, argsExp);

        return lambda.Compile();
    }
}
