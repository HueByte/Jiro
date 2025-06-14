using Jiro.Core.Services.Weather.Models;

namespace Jiro.Core.Services.Geolocation;

public interface IGeolocationService
{
    Task<GeoLocationResponse?> GetGeolocationAsync(string city);
}
