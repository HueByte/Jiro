using System.Text.Json.Serialization;

namespace Jiro.Core.Services.Weather.Models;

/// <summary>
/// Represents the complete weather response from the weather API containing current conditions and hourly forecasts.
/// </summary>
public class WeatherResponse
{
	/// <summary>
	/// Gets or sets the latitude coordinate of the location.
	/// </summary>
	[JsonPropertyName("latitude")]
	public double? Latitude
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the longitude coordinate of the location.
	/// </summary>
	[JsonPropertyName("longitude")]
	public double? Longitude
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the time taken to generate the response in milliseconds.
	/// </summary>
	[JsonPropertyName("generationtime_ms")]
	public double? GenerationtimeMs
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the UTC offset in seconds for the location's timezone.
	/// </summary>
	[JsonPropertyName("utc_offset_seconds")]
	public int? UtcOffsetSeconds
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the timezone identifier for the location.
	/// </summary>
	[JsonPropertyName("timezone")]
	public string? Timezone
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the timezone abbreviation for the location.
	/// </summary>
	[JsonPropertyName("timezone_abbreviation")]
	public string? TimezoneAbbreviation
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the elevation above sea level in meters.
	/// </summary>
	[JsonPropertyName("elevation")]
	public double? Elevation
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the current weather conditions.
	/// </summary>
	[JsonPropertyName("current_weather")]
	public CurrentWeather? CurrentWeather
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the units of measurement for hourly data.
	/// </summary>
	[JsonPropertyName("hourly_units")]
	public HourlyUnits? HourlyUnits
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the hourly weather forecast data.
	/// </summary>
	[JsonPropertyName("hourly")]
	public Hourly? Hourly
	{
		get; set;
	}
}

/// <summary>
/// Represents the current weather conditions at a specific location.
/// </summary>
public class CurrentWeather
{
	/// <summary>
	/// Gets or sets the current temperature.
	/// </summary>
	[JsonPropertyName("temperature")]
	public double? Temperature
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the current wind speed.
	/// </summary>
	[JsonPropertyName("windspeed")]
	public double? Windspeed
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the current wind direction in degrees.
	/// </summary>
	[JsonPropertyName("winddirection")]
	public double? Winddirection
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the weather condition code.
	/// </summary>
	[JsonPropertyName("weathercode")]
	public int? Weathercode
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the timestamp of the current weather reading.
	/// </summary>
	[JsonPropertyName("time")]
	public string? Time
	{
		get; set;
	}
}

/// <summary>
/// Represents hourly weather forecast data including temperature, precipitation, and atmospheric conditions.
/// </summary>
public class Hourly
{
	/// <summary>
	/// Gets or sets the list of hourly timestamps.
	/// </summary>
	[JsonPropertyName("time")]
	public List<string>? Time
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the list of hourly temperatures at 2 meters above ground.
	/// </summary>
	[JsonPropertyName("temperature_2m")]
	public List<double?>? Temperature2m
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the list of hourly rainfall amounts.
	/// </summary>
	[JsonPropertyName("rain")]
	public List<double?>? Rain
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the list of hourly surface pressure readings.
	/// </summary>
	[JsonPropertyName("surface_pressure")]
	public List<double?>? SurfacePressure
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the list of hourly wind speeds at 10 meters above ground.
	/// </summary>
	[JsonPropertyName("windspeed_10m")]
	public List<double?>? Windspeed10m
	{
		get; set;
	}
}

/// <summary>
/// Represents the units of measurement for hourly weather data.
/// </summary>
public class HourlyUnits
{
	/// <summary>
	/// Gets or sets the unit for time values.
	/// </summary>
	[JsonPropertyName("time")]
	public string? Time
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the unit for temperature measurements.
	/// </summary>
	[JsonPropertyName("temperature_2m")]
	public string? Temperature2m
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the unit for rainfall measurements.
	/// </summary>
	[JsonPropertyName("rain")]
	public string? Rain
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the unit for surface pressure measurements.
	/// </summary>
	[JsonPropertyName("surface_pressure")]
	public string? SurfacePressure
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the unit for wind speed measurements.
	/// </summary>
	[JsonPropertyName("windspeed_10m")]
	public string? Windspeed10m
	{
		get; set;
	}
}
