namespace Jiro.Core.Base;

public class CommandResponse<T> where T : ICommandResult
{
    public T? Result { get; set; }
    public string? CommandName { get; set; }
    public CommandType CommandType { get; set; }
}
