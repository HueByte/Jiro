using Jiro.Core.Base.Models;
using Jiro.Core.Base.Results;
using Microsoft.Extensions.DependencyInjection;

namespace Jiro.Core.Base.Models;

/// <summary>
/// Contains necessary information about a command to be executed
/// </summary>
public class CommandInfo
{
    public string Name { get; } = string.Empty;
    public CommandType CommandType { get; }
    public string? CommandSyntax { get; }
    public string? CommandDescription { get; }
    public bool IsAsync { get; } = false;
    public Type Module { get; } = default!;
    public readonly Func<ICommandBase, object?[], Task> Descriptor = default!;
    public IReadOnlyList<ParameterInfo?>? Parameters { get; }

    public CommandInfo(string name, CommandType commandType, bool isAsync, Type container, Func<ICommandBase, object[], Task> descriptor, IReadOnlyList<ParameterInfo> parameters, string? commandSyntax, string? commandDescription)
    {
        Name = name;
        CommandType = commandType;
        IsAsync = isAsync;
        Module = container;
        Descriptor = descriptor;
        Parameters = parameters;
        CommandSyntax = commandSyntax;
        CommandDescription = commandDescription;
    }

    public async Task<CommandResponse> ExecuteAsync(IServiceScope scope, CommandsContainer commandModule, string[] tokens)
    {
        CommandResponse commandResult = new()
        {
            CommandName = Name,
            CommandType = CommandType,
            Result = null
        };

        var instance = scope.ServiceProvider.GetRequiredService(Module);
        object?[] args = ParseArgs(commandModule, tokens);

        if (instance is null)
            throw new CommandException(Name, "Command instance is null");

        if (IsAsync)
        {
            var task = Descriptor((ICommandBase)instance, args);

            if (task is Task<ICommandResult> commandTask)
            {
                commandResult.Result = await commandTask;
            }
            else
            {
                await task;
                commandResult.Result = TextResult.Create("Command executed successfully");
            }
        }
        else
        {
            commandResult.Result = (ICommandResult)Descriptor.Invoke((ICommandBase)instance, args);
        }

        return commandResult;
    }

    private object?[] ParseArgs(CommandsContainer commandModule, string[] tokens)
    {
        object?[] args;

        if (Name == commandModule.DefaultCommand)
        {
            args = new object?[] { string.Join(' ', tokens) };
        }
        else if (Parameters is not null && tokens.Length > 1)
        {
            var paramTokens = tokens[1..];
            args = new object?[Parameters.Count];

            if (args.Length == 1 && Parameters[0]?.ParamType == typeof(string))
            {
                args[0] = string.Join(' ', paramTokens);
                return args;
            }

            for (int i = 0; i < Parameters.Count; i++)
            {
                var param = paramTokens.Length > i ? paramTokens[i] : null;
                if (Parameters[i].Parser is not null)
                    args[i] = Parameters[i].Parser.Parse(param);
            }
        }
        else
        {
            args = Array.Empty<object>();
        }

        return args;
    }
}
