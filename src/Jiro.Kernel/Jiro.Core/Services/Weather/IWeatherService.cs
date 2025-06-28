using Jiro.Core.Services.Weather.Models;

namespace Jiro.Core.Services.Weather;

/// <summary>
/// Defines the contract for weather-related services that provide weather information for specified locations.
/// </summary>
public interface IWeatherService
{
	/// <summary>
	/// Retrieves detailed weather information for the specified city.
	/// </summary>
	/// <param name="city">The name of the city to get weather information for.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the weather response or null if not found.</returns>
	Task<WeatherResponse?> GetWeatherAsync(string city);

	/// <summary>
	/// Retrieves weather information as a formatted string for the specified city.
	/// </summary>
	/// <param name="city">The name of the city to get weather information for.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the weather information as a string or null if not found.</returns>
	Task<string?> GetWeatherStringAsync(string city);
}
