using System.Text.Json.Serialization;

namespace Jiro.Core.Services.Weather.Models;

/// <summary>
/// Represents the root response container for geolocation API responses containing multiple location results.
/// </summary>
public class GeoLocationResponseRoot
{
	/// <summary>
	/// Gets or sets the list of geolocation responses.
	/// </summary>
	public List<GeoLocationResponse>? GeoLocationResponses
	{
		get; set;
	}
}

/// <summary>
/// Represents a geolocation response containing coordinate and address information for a specific location.
/// </summary>
public class GeoLocationResponse
{
	/// <summary>
	/// Gets or sets the unique place identifier.
	/// </summary>
	[JsonPropertyName("place_id")]
	public int PlaceId
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the license information for the data source.
	/// </summary>
	[JsonPropertyName("licence")]
	public string? Licence
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the OpenStreetMap type (node, way, relation).
	/// </summary>
	[JsonPropertyName("osm_type")]
	public string? OsmType
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the OpenStreetMap identifier.
	/// </summary>
	[JsonPropertyName("osm_id")]
	public long OsmId
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the bounding box coordinates for the location.
	/// </summary>
	[JsonPropertyName("boundingbox")]
	public List<string>? Boundingbox
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the latitude coordinate as a string.
	/// </summary>
	[JsonPropertyName("lat")]
	public string? Lat
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the longitude coordinate as a string.
	/// </summary>
	[JsonPropertyName("lon")]
	public string? Lon
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the human-readable display name of the location.
	/// </summary>
	[JsonPropertyName("display_name")]
	public string? DisplayName
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the classification category of the location.
	/// </summary>
	[JsonPropertyName("class")]
	public string? Class
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the specific type of the location within its class.
	/// </summary>
	[JsonPropertyName("type")]
	public string? Type
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the importance score of the location (higher values indicate more important places).
	/// </summary>
	[JsonPropertyName("importance")]
	public double Importance
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the URL to an icon representing the location type.
	/// </summary>
	[JsonPropertyName("icon")]
	public string? Icon
	{
		get; set;
	}
}
