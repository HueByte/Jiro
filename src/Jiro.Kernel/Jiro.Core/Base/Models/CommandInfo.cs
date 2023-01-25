namespace Jiro.Core.Base;

/// <summary>
/// Contains necessary information about a command to be executed
/// </summary>
public class CommandInfo
{
    public string Name { get; } = string.Empty;
    public bool IsAsync { get; } = false;
    public Type Module { get; } = default!;
    public readonly Func<CommandBase, object[], Task> Descriptor = default!;

    public CommandInfo(string name, bool isAsync, Type container, Func<CommandBase, object[], Task> descriptor)
    {
        Name = name;
        IsAsync = isAsync;
        Module = container;
        Descriptor = descriptor;
    }
}
