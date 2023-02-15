using Jiro.Core.Base.Models;

namespace Jiro.Core.Base;

/// <summary>
/// Contains necessary information about a command to be executed
/// </summary>
public class CommandInfo
{
    public string Name { get; } = string.Empty;
    public CommandType CommandType { get; }
    public bool IsAsync { get; } = false;
    public Type Module { get; } = default!;
    public readonly Func<ICommandBase, object[], Task> Descriptor = default!;
    public IReadOnlyList<ParameterInfo> Parameters { get; }

    public CommandInfo(string name, CommandType commandType, bool isAsync, Type container, Func<ICommandBase, object[], Task> descriptor, IReadOnlyList<ParameterInfo> parameters)
    {
        Name = name;
        CommandType = commandType;
        IsAsync = isAsync;
        Module = container;
        Descriptor = descriptor;
        Parameters = parameters;
    }
}
