using System.Net.Http.Json;

using Jiro.Core.Constants;
using Jiro.Core.Services.Weather.Models;

namespace Jiro.Core.Services.Geolocation;

public class GeolocationService : IGeolocationService
{
	private readonly HttpClient _geoClient;

	public GeolocationService (IHttpClientFactory clientFactory)
	{
		_geoClient = clientFactory.CreateClient(HttpClients.GEOLOCATION_CLIENT);
	}

	public async Task<GeoLocationResponse?> GetGeolocationAsync (string city)
	{
		if (string.IsNullOrEmpty(city))
			throw new JiroException(new ArgumentException("city was null or empty", nameof(city)), "Please provide city");

		HttpResponseMessage response = await _geoClient.GetAsync($"search?city={city}&format=json");

		if (response.IsSuccessStatusCode)
		{
			List<GeoLocationResponse?>? result = await response.Content.ReadFromJsonAsync<List<GeoLocationResponse?>>();
			return result?.FirstOrDefault();
		}
		else
		{
			throw new JiroException("Couldn't find the desired city");
		}
	}
}
