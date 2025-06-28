using System.Net.Http.Json;

using Jiro.Core.Constants;
using Jiro.Core.Services.Weather.Models;

namespace Jiro.Core.Services.Geolocation;

/// <summary>
/// Provides geolocation services by integrating with external geolocation APIs to convert city names to geographic coordinates.
/// </summary>
public class GeolocationService : IGeolocationService
{
	private readonly HttpClient _geoClient;

	/// <summary>
	/// Initializes a new instance of the <see cref="GeolocationService"/> class.
	/// </summary>
	/// <param name="clientFactory">The HTTP client factory for creating geolocation API clients.</param>
	public GeolocationService(IHttpClientFactory clientFactory)
	{
		_geoClient = clientFactory.CreateClient(HttpClients.GEOLOCATION_CLIENT);
	}

	/// <summary>
	/// Retrieves geolocation information (latitude and longitude) for the specified city.
	/// </summary>
	/// <param name="city">The name of the city to get coordinates for.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the geolocation response or null if not found.</returns>
	/// <exception cref="JiroException">Thrown when the city parameter is null or empty, when the city cannot be found, or when there's an error fetching geolocation data.</exception>
	public async Task<GeoLocationResponse?> GetGeolocationAsync(string city)
	{
		if (string.IsNullOrWhiteSpace(city))
			throw new JiroException(new ArgumentException("city was null or empty", nameof(city)), "Please provide city");

		try
		{
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
		catch (HttpRequestException ex)
		{
			throw new JiroException(ex, "Error while fetching geolocation data");
		}
		catch (Exception ex)
		{
			throw new JiroException(ex, "An unexpected error occurred while fetching geolocation data");
		}
	}
}
