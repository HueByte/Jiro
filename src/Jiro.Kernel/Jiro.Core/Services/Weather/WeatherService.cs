using System.Net.Http.Json;
using Jiro.Core.Constants;
using Jiro.Core.Interfaces.IServices;
using Jiro.Core.Services.Weather.Models;

namespace Jiro.Core.Services.WeatherService
{
    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _weatherClient;
        private readonly HttpClient _geoClient;
        public WeatherService(IHttpClientFactory clientFactory)
        {
            _weatherClient = clientFactory.CreateClient(HttpClientNames.WEATHER_CLIENT);
            _geoClient = clientFactory.CreateClient(HttpClientNames.GEOLOCATION_CLIENT);
        }

        public async Task<string?> GetWeatherAsync(string city)
        {
            var locationInfo = await GetGeoLocationAsync(city);

            if (locationInfo is null)
                return null;

            Dictionary<string, string> queryParams = new()
            {
                { "latitude", locationInfo.Lat },
                { "longitude", locationInfo.Lon },
                { "current_weather", "true" },
                { "hourly", "temperature_2m,rain,surface_pressure,windspeed_10m" }
            };

            // Build the query string
            string queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));

            var response = await _weatherClient.GetAsync($"forecast?{queryString}");

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<GeoLocationResponse?> GetGeoLocationAsync(string city)
        {
            var response = await _geoClient.GetAsync($"search?city={city}&format=json");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<GeoLocationResponse?>>();
                return result?.FirstOrDefault();
            }

            return null;
        }
    }
}