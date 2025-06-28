using Jiro.Core.Services.Weather.Models;

namespace Jiro.Core.Services.Geolocation;

/// <summary>
/// Defines the contract for geolocation services that provide coordinate information for geographic locations.
/// </summary>
public interface IGeolocationService
{
	/// <summary>
	/// Retrieves geolocation information (latitude and longitude) for the specified city.
	/// </summary>
	/// <param name="city">The name of the city to get coordinates for.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the geolocation response or null if not found.</returns>
	Task<GeoLocationResponse?> GetGeolocationAsync(string city);
}
