namespace Jiro.Core.Base;

public class CommandResponse
{
    public ICommandResult? Result { get; set; }
    public string? CommandName { get; set; }
    public bool IsSuccess { get; set; }
    public List<string> Errors { get; set; } = new();
}
