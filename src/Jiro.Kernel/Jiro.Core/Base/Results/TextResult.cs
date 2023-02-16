namespace Jiro.Core.Base.Results;

public class TextResult : ICommandResult
{
    public string? Response { get; set; }

    private TextResult(string? data)
    {
        Response = data;
    }

    public static TextResult Create(string? data)
    {
        return new TextResult(data);
    }
}
