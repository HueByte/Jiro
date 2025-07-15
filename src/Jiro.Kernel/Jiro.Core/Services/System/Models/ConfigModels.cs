namespace Jiro.Core.Services.System.Models;

/// <summary>
/// System configuration response containing application and environment details.
/// </summary>
public class ConfigResponse
{
	/// <summary>
	/// Gets or sets the name of the application.
	/// </summary>
	public string ApplicationName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the version of the application.
	/// </summary>
	public string? Version { get; set; }

	/// <summary>
	/// Gets or sets the environment in which the application is running.
	/// </summary>
	public string Environment { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the unique identifier for the application instance.
	/// </summary>
	public string InstanceId { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the configuration section details.
	/// </summary>
	public ConfigurationSection Configuration { get; set; } = new();

	/// <summary>
	/// Gets or sets the system information details.
	/// </summary>
	public SystemInfo SystemInfo { get; set; } = new();

	/// <summary>
	/// Gets or sets the uptime of the application.
	/// </summary>
	public TimeSpan Uptime { get; set; }
}

/// <summary>
/// Represents configuration section information.
/// </summary>
public class ConfigurationSection
{
	/// <summary>
	/// Gets or sets the chat configuration.
	/// </summary>
	public object? Chat { get; set; }

	/// <summary>
	/// Gets or sets the logging configuration.
	/// </summary>
	public LoggingConfig Logging { get; set; } = new();

	/// <summary>
	/// Gets or sets the features configuration.
	/// </summary>
	public FeaturesConfig Features { get; set; } = new();
}

/// <summary>
/// Represents logging configuration details.
/// </summary>
public class LoggingConfig
{
	/// <summary>
	/// Gets or sets the log level.
	/// </summary>
	public string? LogLevel { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether console logging is enabled.
	/// </summary>
	public bool EnableConsoleLogging { get; set; } = true;

	/// <summary>
	/// Gets or sets a value indicating whether file logging is enabled.
	/// </summary>
	public bool EnableFileLogging { get; set; } = true;
}

/// <summary>
/// Represents features configuration details.
/// </summary>
public class FeaturesConfig
{
	/// <summary>
	/// Gets or sets a value indicating whether chat is enabled.
	/// </summary>
	public bool ChatEnabled { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether weather features are enabled.
	/// </summary>
	public bool WeatherEnabled { get; set; } = true;

	/// <summary>
	/// Gets or sets a value indicating whether gRPC is enabled.
	/// </summary>
	public bool GrpcEnabled { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether WebSocket is enabled.
	/// </summary>
	public bool WebSocketEnabled { get; set; }
}

/// <summary>
/// Represents system information details.
/// </summary>
public class SystemInfo
{
	/// <summary>
	/// Gets or sets the platform name.
	/// </summary>
	public string Platform { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the operating system version.
	/// </summary>
	public string OsVersion { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the .NET version.
	/// </summary>
	public string DotnetVersion { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the working directory path.
	/// </summary>
	public string WorkingDirectory { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the machine name.
	/// </summary>
	public string MachineName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the number of processors.
	/// </summary>
	public int ProcessorCount { get; set; }

	/// <summary>
	/// Gets or sets the total memory available.
	/// </summary>
	public long TotalMemory { get; set; }
}

/// <summary>
/// Represents the response for configuration updates.
/// </summary>
public class ConfigUpdateResponse
{
	/// <summary>
	/// Gets or sets a value indicating whether the update was successful.
	/// </summary>
	public bool Success { get; set; }

	/// <summary>
	/// Gets or sets the message associated with the update.
	/// </summary>
	public string Message { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the keys received during the update.
	/// </summary>
	public string[] ReceivedKeys { get; set; } = Array.Empty<string>();

	/// <summary>
	/// Gets or sets additional notes regarding the update.
	/// </summary>
	public string Note { get; set; } = string.Empty;
}
