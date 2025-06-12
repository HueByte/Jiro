using Jiro.Core.Services.Weather.Models;

namespace Jiro.Core.Interfaces.IServices;

public interface IGeolocationService
{
    Task<GeoLocationResponse?> GetGeolocationAsync(string city);
}
