namespace Jiro.Core.Base;

public class CommandResult : ICommandResult
{
    public object? Data { get; set; }

    private CommandResult(object? data)
    {
        Data = data;
    }

    public static CommandResult Create(object? data)
    {
        return new CommandResult(data);
    }
}
