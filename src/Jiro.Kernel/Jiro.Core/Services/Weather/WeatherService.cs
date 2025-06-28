using System.Text.Json;

using Jiro.Core.Constants;
using Jiro.Core.Services.Geolocation;
using Jiro.Core.Services.Weather.Models;

namespace Jiro.Core.Services.Weather;

/// <summary>
/// Provides weather information services by integrating with external weather APIs and geolocation services.
/// </summary>
public class WeatherService : IWeatherService
{
	private readonly HttpClient _weatherClient;
	private readonly IGeolocationService _geolocationService;

	/// <summary>
	/// Initializes a new instance of the <see cref="WeatherService"/> class.
	/// </summary>
	/// <param name="clientFactory">The HTTP client factory for creating weather API clients.</param>
	/// <param name="geolocationService">The geolocation service for converting city names to coordinates.</param>
	public WeatherService(IHttpClientFactory clientFactory, IGeolocationService geolocationService)
	{
		_weatherClient = clientFactory.CreateClient(HttpClients.WEATHER_CLIENT);
		_geolocationService = geolocationService;
	}

	/// <summary>
	/// Retrieves weather information as a JSON string for the specified city.
	/// </summary>
	/// <param name="city">The name of the city to get weather information for.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the weather information as a JSON string or null if not found.</returns>
	/// <exception cref="JiroException">Thrown when the city cannot be found or weather data is unavailable.</exception>
	public async Task<string?> GetWeatherStringAsync(string city)
	{
		var locationInfo = await _geolocationService.GetGeolocationAsync(city);

		if (locationInfo is null || locationInfo.Lat is null || locationInfo.Lon is null)
			throw new JiroException("Couldn't find he weather for desired city");

		Dictionary<string, string> queryParams = new()
		{
			{ "latitude", locationInfo.Lat },
			{ "longitude", locationInfo.Lon },
			{ "current_weather", "true" },
			{ "hourly", "temperature_2m,rain,surface_pressure,windspeed_10m" }
		};

		// Build the query string
		string queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));

		var response = await _weatherClient.GetAsync($"forecast?{queryString}");

		return await response.Content.ReadAsStringAsync();
	}

	/// <summary>
	/// Retrieves structured weather information for the specified city.
	/// </summary>
	/// <param name="city">The name of the city to get weather information for.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the deserialized weather response or null if not found.</returns>
	public async Task<WeatherResponse?> GetWeatherAsync(string city)
	{
		WeatherResponse? response = null;

		var weather = await GetWeatherStringAsync(city);

		if (weather is not null)
			response = JsonSerializer.Deserialize<WeatherResponse>(weather);

		return response;
	}
}
