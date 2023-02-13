using System.Text.Json.Serialization;

namespace Jiro.Core.Base;

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
