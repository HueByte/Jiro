using Jiro.Core.Services.Weather.Models;

namespace Jiro.Core.Interfaces.IServices
{
    public interface IWeatherService
    {
        Task<string?> GetWeatherStringAsync(string country);
        Task<WeatherResponse?> GetWeatherAsync(string country);
    }
}