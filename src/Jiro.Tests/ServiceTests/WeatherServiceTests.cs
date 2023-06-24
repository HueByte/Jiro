using Jiro.Core;
using Jiro.Core.Constants;
using Jiro.Core.Interfaces.IServices;
using Jiro.Core.Services.Weather;
using Jiro.Core.Services.Weather.Models;
using Moq;
using RichardSzalay.MockHttp;
using System.Text.Json;

namespace Jiro.Tests.ServiceTests
{
    public class WeatherServiceTests
    {
        private readonly IWeatherService _weatherService;
        private readonly Mock<IGeolocationService> _geolocationServiceMock;
        private readonly Mock<IHttpClientFactory> _httpClientFactory;
        private const string _city = "London";
        private const string _lat = "51.5074";
        private const string _lon = "-0.1274";
        private const string _weatherBaseAddress = "https://api.open-meteo.com/v1";
        private static string WeatherEndpoint(string lat, string lon) => $"https://api.open-meteo.com/forecast?latitude={_lat}&longitude={_lon}&current_weather=true&hourly=temperature_2m,rain,surface_pressure,windspeed_10m";

        public WeatherServiceTests()
        {
            // weather
            WeatherResponse correctWeatherResponse = new()
            {
                Latitude = Convert.ToDouble(_lat),
                Longitude = Convert.ToDouble(_lon),
                GenerationtimeMs = 0.0,
                UtcOffsetSeconds = 0,
                Timezone = "Europe/London",
                TimezoneAbbreviation = "BST",
                Elevation = 0.0,
                CurrentWeather = new CurrentWeather
                {
                    Temperature = 16.0,
                    Windspeed = 3.0,
                    Winddirection = 0.0,
                    Weathercode = 800,
                    Time = "2021-09-05T18:00:00Z"
                },
                HourlyUnits = new HourlyUnits
                {
                    Temperature2m = "°C",
                    Rain = "mm",
                    Windspeed10m = "m/s"
                },
                Hourly = new Hourly
                {
                    Time = new List<string> { "2021-09-05T18:00:00Z", "2021-09-05T19:00:00Z", "2021-09-05T20:00:00Z", "2021-09-05T21:00:00Z", "2021-09-05T22:00:00Z", "2021-09-05T23:00:00Z", "2021-09-06T00:00:00Z", "2021-09-06T01:00:00Z", "2021-09-06T02:00:00Z", "2021-09-06T03:00:00Z", "2021-09-06T04:00:00Z", "2021-09-06T05:00:00Z", "2021-09-06T06:00:00Z", "2021-09-06T07:00:00Z", "2021-09-06T08:00:00Z", "2021-09-06T09:00:00Z", "2021-09-06T10:00:00Z", "2021-09-06T11:00:00Z", "2021-09-06T12:00:00Z", "2021-09-06T13:00:00Z", "2021-09-06T14:00:00Z" }
                }
            };
            
            MockHttpMessageHandler weatherHttpMock = new();
            weatherHttpMock.When(WeatherEndpoint(_lat, _lon))
                .Respond("application/json", JsonSerializer.Serialize(correctWeatherResponse));

            weatherHttpMock.When(WeatherEndpoint("", ""))
                .Respond("application/json", "{}");

            HttpClient weatherClient = weatherHttpMock.ToHttpClient();
            weatherClient.BaseAddress = new Uri(_weatherBaseAddress);

            // geolocation service
            _geolocationServiceMock = new();
            _geolocationServiceMock.Setup(_ => _.GetGeolocationAsync(_city)).ReturnsAsync(new GeoLocationResponse { Lat = _lat, Lon = _lon });

            // client factory
            _httpClientFactory = new Mock<IHttpClientFactory>();
            _httpClientFactory.Setup(_ => _.CreateClient(HttpClients.WEATHER_CLIENT)).Returns(weatherClient);

            _weatherService = new WeatherService(_httpClientFactory.Object, _geolocationServiceMock.Object);
        }

        [Fact]
        public async Task GetWeatherStringAsync_WithCorrectCity()
        {
            // Act
            var result = await _weatherService.GetWeatherStringAsync(_city);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
            Assert.True(result.StartsWith("{") && result.EndsWith("}"));
        }

        [Fact]
        public async Task GetWeatherStringAsync_WithWrongCity()
        {
            // Act
            var result = await _weatherService.GetWeatherStringAsync("WrongCity");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetWeatherAsync_WithCorrectCity()
        {
            // Act
            var result = await _weatherService.GetWeatherAsync(_city);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.CurrentWeather);
            Assert.NotNull(result.Hourly);
            Assert.NotNull(result.HourlyUnits);
            Assert.NotNull(result.Hourly.Time);
            Assert.True(result.Hourly.Time.Count > 0);
        }

        [Fact]
        public async Task GetWeatherAsync_WithWrongCity()
        {
            // Act
            var result = await _weatherService.GetWeatherAsync("WrongCity");

            // Assert
            Assert.Null(result);
        }
    }
}
