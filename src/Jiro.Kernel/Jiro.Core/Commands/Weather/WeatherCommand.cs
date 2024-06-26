using System.Text.Json;
using Jiro.Core.Services.Weather.Models;

namespace Jiro.Core.Commands.Weather;

[CommandModule("Weather")]
public class WeatherCommand : ICommandBase
{
    private readonly IWeatherService _weatherService;
    public WeatherCommand(IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    [Command("weather", CommandType.Graph, "weather \"Location\" [daysRange]", "Shows weather forecast for the specified location (24 hours by default)")]
    public async Task<ICommandResult> Weather(string location, int daysRange)
    {
        if (daysRange <= 0 || daysRange > 7) daysRange = 1;
        int range = daysRange * 24;

        // fetch weather data
        var weather = await _weatherService.GetWeatherAsync(location);

        if (weather is null || weather.Hourly is null || weather.Hourly.Time is null)
            return GraphResult.Create("No weather data found", null, null!);

        // convert to acceptable format [{...}, {...}, {...}] 
        var data = weather.Hourly.Time
            .Select((time, index) =>
                new WeatherGraphData
                {
                    Date = time,
                    Temperature = weather.Hourly.Temperature2m[index],
                    Rain = weather.Hourly.Rain[index],
                    WindSpeed = weather.Hourly.Windspeed10m[index]
                })
            .Take(range > weather.Hourly.Time.Count ? weather.Hourly.Time.Count : range);

        // create units dictionary
        Dictionary<string, string> units = new()
        {
            { "temperature", weather.HourlyUnits.Temperature2m },
            { "rain", weather.HourlyUnits.Rain },
            { "windSpeed", weather.HourlyUnits.Windspeed10m },
        };

        // create note
        var note = $"Current weather in {location} is {weather.CurrentWeather.Temperature} {weather.HourlyUnits.Temperature2m} with wind of {weather.CurrentWeather.Windspeed} {weather.HourlyUnits.Windspeed10m}";
        var dataInJson = JsonSerializer.Serialize(data);
        return GraphResult.Create("", dataInJson, units, "date", note: note);
    }
}