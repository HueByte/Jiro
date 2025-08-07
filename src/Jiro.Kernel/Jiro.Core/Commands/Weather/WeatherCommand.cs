using System.Text.Json;

using Jiro.Core.Services.MessageCache;
using Jiro.Core.Services.Weather;
using Jiro.Core.Services.Weather.Models;

namespace Jiro.Core.Commands.Weather;

/// <summary>
/// Command module that provides weather forecast functionality with graphical data visualization.
/// </summary>
[CommandModule("Weather")]
public class WeatherCommand : ICommandBase
{
	/// <summary>
	/// The weather service for retrieving weather data.
	/// </summary>
	private readonly IWeatherService _weatherService;

	/// <summary>
	/// The message manager for handling weather-related messages.
	/// </summary>
	private readonly IMessageManager _messageManager;
	/// <summary>
	/// Initializes a new instance of the WeatherCommand class.
	/// </summary>
	/// <param name="weatherService">The weather service for retrieving weather data.</param>
	/// <param name="messageManager">The message manager for handling messages.</param>
	/// <exception cref="ArgumentNullException">Thrown when any of the required parameters is null.</exception>
	public WeatherCommand(IWeatherService weatherService, IMessageManager messageManager)
	{
		_weatherService = weatherService ?? throw new ArgumentNullException(nameof(weatherService), "Weather service cannot be null.");
		_messageManager = messageManager ?? throw new ArgumentNullException(nameof(messageManager), "Message manager cannot be null.");
	}

	/// <summary>
	/// Retrieves and displays weather forecast data for the specified location as a graph.
	/// </summary>
	/// <param name="location">The location for which to retrieve weather data.</param>
	/// <param name="daysRange">The number of days to include in the forecast (1-7 days, defaults to 1).</param>
	/// <returns>A task representing the asynchronous operation that returns weather graph data.</returns>
	[Command("weather", CommandType.Graph, "weather \"Location\" [daysRange]", "Shows weather forecast for the specified location (24 hours by default)")]
	public async Task<ICommandResult> Weather(string location, int daysRange)
	{
		// Validate and adjust daysRange
		daysRange = Math.Clamp(daysRange, 1, 7);
		int range = daysRange * 24;

		// Fetch weather data
		var weather = await _weatherService.GetWeatherAsync(location);
		if (weather?.Hourly?.Time == null ||
			weather.Hourly.Temperature2m == null ||
			weather.Hourly.Rain == null ||
			weather.Hourly.Windspeed10m == null ||
			weather.HourlyUnits == null)
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
			["temperature"] = weather.HourlyUnits.Temperature2m ?? "°C",
			["rain"] = weather.HourlyUnits.Rain ?? "mm",
			["windSpeed"] = weather.HourlyUnits.Windspeed10m ?? "km/h"
		};

		// Create note
		var currentWeatherNote = weather.CurrentWeather != null
			? $"Current weather in {location} is {weather.CurrentWeather.Temperature} {weather.HourlyUnits.Temperature2m ?? "°C"} with wind of {weather.CurrentWeather.Windspeed} {weather.HourlyUnits.Windspeed10m ?? "km/h"}"
			: $"Weather data for {location}";
		var note = currentWeatherNote;

		// Serialize data and return result
		var dataInJson = JsonSerializer.Serialize(data);
		return GraphResult.Create("", dataInJson, units, "date", note: note);
	}
}
