namespace Jiro.Core.Base.Models;

public class CommandResponse
{
    public ICommandResult? Result { get; set; }
    public string? CommandName { get; set; }
    public CommandType CommandType { get; set; }
}
