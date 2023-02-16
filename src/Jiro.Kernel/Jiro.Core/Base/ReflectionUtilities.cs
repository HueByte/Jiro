using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Jiro.Core.Base.Attributes;
using Jiro.Core.Base.Models;
using Jiro.Core.Base.TypeParsers;

namespace Jiro.Core.Base
{
    internal static class ReflectionUtilities
    {
        internal static Assembly[]? GetDomainAssemblies() => AppDomain.CurrentDomain.GetAssemblies();

        internal static Type[]? GetCommandModules(Assembly[] assemblies)
        {
            var commandModules = assemblies
                .SelectMany(asm => asm.GetTypes()
                    .Where(type =>
                        !type.IsInterface
                        && type.GetCustomAttributes(typeof(CommandModuleAttribute), false).Length > 0
                ))
                .ToArray();

            return commandModules;
        }

        internal static MethodInfo[] GetPotentialCommands(Type type)
        {
            var methodInfos = type
                .GetMethods()
                .Where(method => method.GetCustomAttributes(typeof(CommandAttribute), false).Length > 0)
                .ToArray();

            return methodInfos;
        }

        internal static CommandInfo? BuildCommandFromMethodInfo<TBaseInstance, TReturn>(MethodInfo method)
        {
            if (method is null) return null;

            var delcaringType = method.DeclaringType;
            if (delcaringType is null) return null;

            var commandName = method.GetCustomAttribute<CommandAttribute>()?.CommandName.ToLower() ?? "";
            var commandType = method.GetCustomAttribute<CommandAttribute>()?.CommandType ?? CommandType.Text;
            var isAsync = method.GetCustomAttribute<AsyncStateMachineAttribute>() is not null;
            var compiledMethod = CompileMethodInvoker<TBaseInstance, TReturn>(method);
            var args = GetParameters(method);

            CommandInfo commandInfo = new(
                commandName,
                commandType,
                isAsync,
                delcaringType,
                compiledMethod as Func<ICommandBase, object[], Task>,
                args
            );

            return commandInfo;
        }

        internal static Func<TInstance, object[], TReturn> CompileMethodInvoker<TInstance, TReturn>(MethodInfo method)
        {
            var parameters = method.GetParameters();
            var paramsExp = new Expression[parameters.Length];

            // set first param as Module instance that's fetched from DI container
            var instanceExp = Expression.Parameter(typeof(TInstance), "instance");
            var argsExp = Expression.Parameter(typeof(object[]), "args");

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                var indexExp = Expression.Constant(i);
                var accessExp = Expression.ArrayIndex(argsExp, indexExp);
                paramsExp[i] = Expression.Convert(accessExp, parameter.ParameterType);
            }

            var callExp = Expression.Call(Expression.Convert(instanceExp, method.ReflectedType!), method, paramsExp);
            var finalExp = Expression.Convert(callExp, typeof(TReturn));
            var lambda = Expression.Lambda<Func<TInstance, object[], TReturn>>(finalExp, instanceExp, argsExp);

            return lambda.Compile();
        }

        internal static IReadOnlyList<Models.ParameterInfo> GetParameters(MethodInfo methodInfo)
        {
            List<Models.ParameterInfo> parameterInfos = new();

            var parameters = methodInfo.GetParameters();

            foreach (var parameter in parameters)
            {
                Models.ParameterInfo parameterInfo = new(parameter.ParameterType, GetParser(parameter.ParameterType)!);
                parameterInfos.Add(parameterInfo);
            }

            return parameterInfos;
        }

        private static TypeParser? GetParser(Type type)
        {
            // todo
            return type switch
            {
                _ => (TypeParser)Activator.CreateInstance(typeof(DefaultValueParser<>).MakeGenericType(new Type[] { type }))!
            };
        }
    }
}