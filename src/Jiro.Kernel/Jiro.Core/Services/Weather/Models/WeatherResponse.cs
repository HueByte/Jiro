using System.Text.Json.Serialization;

namespace Jiro.Core.Services.Weather.Models;

public class WeatherResponse
{
    [JsonPropertyName("latitude")]
    public double? Latitude
    {
        get; set;
    }

    [JsonPropertyName("longitude")]
    public double? Longitude
    {
        get; set;
    }

    [JsonPropertyName("generationtime_ms")]
    public double? GenerationtimeMs
    {
        get; set;
    }

    [JsonPropertyName("utc_offset_seconds")]
    public int? UtcOffsetSeconds
    {
        get; set;
    }

    [JsonPropertyName("timezone")]
    public string? Timezone
    {
        get; set;
    }

    [JsonPropertyName("timezone_abbreviation")]
    public string? TimezoneAbbreviation
    {
        get; set;
    }

    [JsonPropertyName("elevation")]
    public double? Elevation
    {
        get; set;
    }

    [JsonPropertyName("current_weather")]
    public CurrentWeather? CurrentWeather
    {
        get; set;
    }

    [JsonPropertyName("hourly_units")]
    public HourlyUnits? HourlyUnits
    {
        get; set;
    }

    [JsonPropertyName("hourly")]
    public Hourly? Hourly
    {
        get; set;
    }
}

public class CurrentWeather
{
    [JsonPropertyName("temperature")]
    public double? Temperature
    {
        get; set;
    }

    [JsonPropertyName("windspeed")]
    public double? Windspeed
    {
        get; set;
    }

    [JsonPropertyName("winddirection")]
    public double? Winddirection
    {
        get; set;
    }

    [JsonPropertyName("weathercode")]
    public int? Weathercode
    {
        get; set;
    }

    [JsonPropertyName("time")]
    public string? Time
    {
        get; set;
    }
}

public class Hourly
{
    [JsonPropertyName("time")]
    public List<string>? Time
    {
        get; set;
    }

    [JsonPropertyName("temperature_2m")]
    public List<double?>? Temperature2m
    {
        get; set;
    }

    [JsonPropertyName("rain")]
    public List<double?>? Rain
    {
        get; set;
    }

    [JsonPropertyName("surface_pressure")]
    public List<double?>? SurfacePressure
    {
        get; set;
    }

    [JsonPropertyName("windspeed_10m")]
    public List<double?>? Windspeed10m
    {
        get; set;
    }
}

public class HourlyUnits
{
    [JsonPropertyName("time")]
    public string? Time
    {
        get; set;
    }

    [JsonPropertyName("temperature_2m")]
    public string? Temperature2m
    {
        get; set;
    }

    [JsonPropertyName("rain")]
    public string? Rain
    {
        get; set;
    }

    [JsonPropertyName("surface_pressure")]
    public string? SurfacePressure
    {
        get; set;
    }

    [JsonPropertyName("windspeed_10m")]
    public string? Windspeed10m
    {
        get; set;
    }
}
