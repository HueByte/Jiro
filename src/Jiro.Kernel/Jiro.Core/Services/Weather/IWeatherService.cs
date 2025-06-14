using Jiro.Core.Services.Weather.Models;

namespace Jiro.Core.Services.Weather;

public interface IWeatherService
{
    Task<WeatherResponse?> GetWeatherAsync(string city);
    Task<string?> GetWeatherStringAsync(string city);
}
