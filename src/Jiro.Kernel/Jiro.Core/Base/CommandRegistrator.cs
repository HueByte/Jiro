using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Jiro.Core.Base.Attributes;
using Jiro.Core.Base.Models;
using Jiro.Core.Commands.GPT;
using Microsoft.Extensions.DependencyInjection;

namespace Jiro.Core.Base;

public static class CommandRegistrator
{
    public static IServiceCollection RegisterCommands(this IServiceCollection services)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        // types that contain CommandContainer attribute
        var commandModules = assemblies
            .SelectMany(asm => asm.GetTypes()
                .Where(type =>
                    !type.IsInterface
                    && type.GetCustomAttributes(typeof(CommandModuleAttribute), false).Length > 0
            ))
            .ToList();


        List<CommandModuleInfo> commandModulesInfos = new();
        foreach (var container in commandModules)
        {
            CommandModuleInfo commandModuleInfo = new();
            var commandsInfos = GetCommands(container);
            commandModuleInfo.SetCommands(commandsInfos);
            commandModuleInfo.SetName(container.GetCustomAttribute<CommandModuleAttribute>()?.ModuleName ?? "");

            services.AddScoped(container);
            commandModulesInfos.Add(commandModuleInfo);
        };

        CommandsContainer globalContainer = new();

        // add default command attribute
        globalContainer.SetDefaultCommand(nameof(GPTCommand.Chat).ToLower());
        globalContainer.AddModules(commandModulesInfos);
        globalContainer.AddCommands(commandModulesInfos
            .SelectMany(x => x.Commands.Select(e => e.Value))
            .ToList());

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

        // iterate thru all methods and extract command info
        foreach (var methodInfo in methodInfos)
        {
            if (methodInfo is null) continue;

            var delcaringType = methodInfo.DeclaringType;

            if (delcaringType is null) continue;

            var compiledLambda = CompileMethodInvoker(methodInfo!);

            var args = GetParameters(methodInfo);

            CommandInfo commandInfo = new(
                methodInfo.GetCustomAttribute<CommandAttribute>()?.CommandName.ToLower() ?? "",
                methodInfo.GetCustomAttribute<CommandAttribute>()?.CommandType ?? CommandType.Text,
                methodInfo.GetCustomAttribute<AsyncStateMachineAttribute>() is not null,
                delcaringType,
                compiledLambda as Func<ICommandBase, object[], Task>,
                args
            );

            commandInfos.Add(commandInfo);
        }

        return commandInfos;
    }

    private static Func<ICommandBase, object[], Task> CompileMethodInvoker(MethodInfo methodInfo)
    {
        var parameters = methodInfo.GetParameters();
        var paramsExp = new Expression[parameters.Length];

        // set first param as Module instance that's fetched from DI container
        var instanceExp = Expression.Parameter(typeof(ICommandBase), "instance");
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
        var lambda = Expression.Lambda<Func<ICommandBase, object[], Task>>(finalExp, instanceExp, argsExp);

        return lambda.Compile();
    }

    private static IReadOnlyList<Models.ParameterInfo> GetParameters(MethodInfo methodInfo)
    {
        List<Models.ParameterInfo> parameterInfos = new();

        var parameters = methodInfo.GetParameters();

        foreach (var parameter in parameters)
        {
            parameterInfos.Add(new Models.ParameterInfo(parameter.ParameterType));
        }

        return parameterInfos;
    }
}
