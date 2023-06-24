using Jiro.Core.Services.Weather.Models;

namespace Jiro.Core;

public interface IGeolocationService
{
    Task<GeoLocationResponse?> GetGeolocationAsync(string city);
}
