namespace Jiro.Core.Services.Configuration.Models;

/// <summary>
/// System configuration response
/// </summary>
public class SystemConfigResponse
{
	/// <summary>
	/// Gets or sets the name of the application.
	/// </summary>
	public string ApplicationName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the application version.
	/// </summary>
	public string? Version { get; set; }

	/// <summary>
	/// Gets or sets the deployment environment (e.g., Development, Production).
	/// </summary>
	public string Environment { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the unique identifier of the application instance.
	/// </summary>
	public string? InstanceId { get; set; }

	/// <summary>
	/// Gets or sets the application configuration details.
	/// </summary>
	public ConfigurationSection Configuration { get; set; } = new();

	/// <summary>
	/// Gets or sets the system information details.
	/// </summary>
	public SystemInfo SystemInfo { get; set; } = new();

	/// <summary>
	/// Gets or sets the application uptime.
	/// </summary>
	public TimeSpan Uptime { get; set; }
}

/// <summary>
/// Configuration section details
/// </summary>
public class ConfigurationSection
{
	/// <summary>
	/// Gets or sets the chat configuration settings.
	/// </summary>
	public object? Chat { get; set; }

	/// <summary>
	/// Gets or sets the features configuration settings.
	/// </summary>
	public FeaturesConfig Features { get; set; } = new();
}


/// <summary>
/// Features configuration
/// </summary>
public class FeaturesConfig
{
	/// <summary>
	/// Gets or sets a value indicating whether chat functionality is enabled.
	/// </summary>
	public bool ChatEnabled { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether weather functionality is enabled.
	/// </summary>
	public bool WeatherEnabled { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether gRPC functionality is enabled.
	/// </summary>
	public bool GrpcEnabled { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether WebSocket functionality is enabled.
	/// </summary>
	public bool WebSocketEnabled { get; set; }
}

/// <summary>
/// System information
/// </summary>
public class SystemInfo
{
	/// <summary>
	/// Gets or sets the operating system platform.
	/// </summary>
	public string Platform { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the operating system version.
	/// </summary>
	public string OsVersion { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the .NET runtime version.
	/// </summary>
	public string DotnetVersion { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the current working directory of the application.
	/// </summary>
	public string WorkingDirectory { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the name of the machine hosting the application.
	/// </summary>
	public string MachineName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the number of logical processors on the machine.
	/// </summary>
	public int ProcessorCount { get; set; }

	/// <summary>
	/// Gets or sets the total amount of system memory in bytes.
	/// </summary>
	public long TotalMemory { get; set; }
}

/// <summary>
/// Configuration update response
/// </summary>
public class ConfigUpdateResponse
{
	/// <summary>
	/// Gets or sets a value indicating whether the configuration update was successful.
	/// </summary>
	public bool Success { get; set; }

	/// <summary>
	/// Gets or sets the response message describing the result of the update operation.
	/// </summary>
	public string Message { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the array of configuration keys that were received in the update request.
	/// </summary>
	public string[] ReceivedKeys { get; set; } = Array.Empty<string>();

	/// <summary>
	/// Gets or sets an additional note about the configuration update.
	/// </summary>
	public string Note { get; set; } = string.Empty;
}
