using System.Text.Json;

using Jiro.Core.Constants;
using Jiro.Core.Services.Geolocation;
using Jiro.Core.Services.Weather.Models;

namespace Jiro.Core.Services.Weather;

public class WeatherService : IWeatherService
{
	private readonly HttpClient _weatherClient;
	private readonly IGeolocationService _geolocationService;
	public WeatherService(IHttpClientFactory clientFactory, IGeolocationService geolocationService)
	{
		_weatherClient = clientFactory.CreateClient(HttpClients.WEATHER_CLIENT);
		_geolocationService = geolocationService;
	}

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

	public async Task<WeatherResponse?> GetWeatherAsync(string city)
	{
		WeatherResponse? response = null;

		var weather = await GetWeatherStringAsync(city);

		if (weather is not null)
			response = JsonSerializer.Deserialize<WeatherResponse>(weather);

		return response;
	}
}
