using Jiro.Core.Services.Weather.Models;

namespace Jiro.Core.Interfaces.IServices
{
    public interface IWeatherService
    {
        Task<string?> GetWeatherAsync(string country);
        Task<GeoLocationResponse?> GetGeoLocationAsync(string city);
    }
}