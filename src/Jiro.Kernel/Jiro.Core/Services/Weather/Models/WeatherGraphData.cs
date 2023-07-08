namespace Jiro.Core.Services.Weather.Models;

public class WeatherGraphData
{
    public string Date { get; set; } = string.Empty;
    public double? Temperature { get; set; }
    public double? WindSpeed { get; set; }
    public double? Rain { get; set; }
}