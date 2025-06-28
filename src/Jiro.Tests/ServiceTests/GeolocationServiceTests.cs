using System.Text.Json;

using Jiro.Core;
using Jiro.Core.Constants;
using Jiro.Core.Services.Geolocation;
using Jiro.Core.Services.Weather.Models;

using Moq;

using RichardSzalay.MockHttp;

namespace Jiro.Tests.ServiceTests;

public class GeolocationServiceTests
{
	private readonly Mock<IHttpClientFactory> _httpClientFactory;
	private readonly IGeolocationService _geolocationService;
	private const string _city = "London";
	private const string _geoBaseAddress = "https://nominatim.openstreetmap.org/";
	private static string GeoEndpoint(string city) => $"https://nominatim.openstreetmap.org/search?city={city}&format=json";

	public GeolocationServiceTests()
	{
		// geo
		MockHttpMessageHandler geoHttpMock = new();
		geoHttpMock.When(GeoEndpoint(_city))
			.Respond("application/json", JsonSerializer.Serialize(new List<GeoLocationResponse> { new() }));

		geoHttpMock.When(GeoEndpoint(""))
			.Respond("application/json", "{}");

		HttpClient geoClient = geoHttpMock.ToHttpClient();
		geoClient.BaseAddress = new Uri(_geoBaseAddress);

		// client factory
		_httpClientFactory = new Mock<IHttpClientFactory>();
		_httpClientFactory.Setup(_ => _.CreateClient(HttpClients.GEOLOCATION_CLIENT)).Returns(geoClient);

		_geolocationService = new GeolocationService(_httpClientFactory.Object);
	}

	[Fact]
	public async Task GetGeolocationAsync_WithCorrectCity()
	{
		// Act
		var result = await _geolocationService.GetGeolocationAsync(_city);

		// Assert
		Assert.NotNull(result);
	}

	[Theory]
	[InlineData("WrongCity")]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData(null)]
	public async Task GetGeolocationAsync_WithWrongCity(string? city)
	{
		// Act & Asssert
		await Assert.ThrowsAsync<JiroException>(async () => await _geolocationService.GetGeolocationAsync(city));
	}
}
