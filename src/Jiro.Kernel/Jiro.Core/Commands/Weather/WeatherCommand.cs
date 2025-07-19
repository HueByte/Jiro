using System.Text.Json;

using Jiro.Core.Services.MessageCache;
using Jiro.Core.Services.Weather;
using Jiro.Core.Services.Weather.Models;

namespace Jiro.Core.Commands.Weather;

[CommandModule("Weather")]
public class WeatherCommand : ICommandBase
{
	private readonly IWeatherService _weatherService;
	private readonly IMessageManager _messageManager;
	public WeatherCommand(IWeatherService weatherService, IMessageManager messageManager)
	{
		_weatherService = weatherService ?? throw new ArgumentNullException(nameof(weatherService), "Weather service cannot be null.");
		_messageManager = messageManager ?? throw new ArgumentNullException(nameof(messageManager), "Message manager cannot be null.");
	}

	[Command("weather", CommandType.Graph, "weather \"Location\" [daysRange]", "Shows weather forecast for the specified location (24 hours by default)")]
	public async Task<ICommandResult> Weather(string location, int daysRange)
	{
		// Validate and adjust daysRange
		daysRange = Math.Clamp(daysRange, 1, 7);
		int range = daysRange * 24;

		// Fetch weather data
		var weather = await _weatherService.GetWeatherAsync(location);
		if (weather?.Hourly?.Time == null)
		{
			return GraphResult.Create("No weather data found", null, null!);
		}

		// Prepare weather data for graph
		var data = weather.Hourly.Time
			.Zip(weather.Hourly.Temperature2m, static (time, temp) => new { time, temp })
			.Zip(weather.Hourly.Rain, static (prev, rain) => new { prev.time, prev.temp, rain })
			.Zip(weather.Hourly.Windspeed10m, static (prev, windSpeed) => new WeatherGraphData
			{
				Date = prev.time,
				Temperature = prev.temp,
				Rain = prev.rain,
				WindSpeed = windSpeed
			})
			.Take(Math.Min(range, weather.Hourly.Time.Count));

		// Create units dictionary
		var units = new Dictionary<string, string>
		{
			["temperature"] = weather.HourlyUnits.Temperature2m,
			["rain"] = weather.HourlyUnits.Rain,
			["windSpeed"] = weather.HourlyUnits.Windspeed10m
		};

		// Create note
		var note = $"Current weather in {location} is {weather.CurrentWeather.Temperature} {weather.HourlyUnits.Temperature2m} with wind of {weather.CurrentWeather.Windspeed} {weather.HourlyUnits.Windspeed10m}";

		// Serialize data and return result
		var dataInJson = JsonSerializer.Serialize(data);
		return GraphResult.Create("", dataInJson, units, "date", note: note);
	}
}
