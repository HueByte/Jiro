using System.Security;
using System.Text.Json.Serialization;

namespace Jiro.Core.Base.Result;

public class GraphResult : ICommandResult
{
    public object? Data { get; set; }
    public Dictionary<string, string>? Units { get; set; }
    public string? Note { get; set; } = string.Empty;
    public string? XAxis { get; set; } = string.Empty;
    public string? YAxis { get; set; } = string.Empty;

    private GraphResult(object? data, Dictionary<string, string> units, string? xAxis = null, string? yAxis = null, string? note = null)
    {
        Data = data;
        Units = units;
        XAxis = xAxis;
        YAxis = yAxis;
        Note = note;
    }

    public static GraphResult Create(object? data, Dictionary<string, string> units, string? xAxis = null, string? yAxis = null, string? note = null)
    {
        return new GraphResult(data, units, xAxis, yAxis, note);
    }
}