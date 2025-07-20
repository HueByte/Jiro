namespace Jiro.Core.Services.Configuration.Models;

/// <summary>
/// System configuration response
/// </summary>
public class SystemConfigResponse
{
	public string ApplicationName { get; set; } = string.Empty;
	public string? Version { get; set; }
	public string Environment { get; set; } = string.Empty;
	public string? InstanceId { get; set; }
	public ConfigurationSection Configuration { get; set; } = new();
	public SystemInfo SystemInfo { get; set; } = new();
	public TimeSpan Uptime { get; set; }
}

/// <summary>
/// Configuration section details
/// </summary>
public class ConfigurationSection
{
	public object? Chat { get; set; }
	public LoggingConfig Logging { get; set; } = new();
	public FeaturesConfig Features { get; set; } = new();
}

/// <summary>
/// Logging configuration
/// </summary>
public class LoggingConfig
{
	public string? LogLevel { get; set; }
	public bool EnableConsoleLogging { get; set; }
	public bool EnableFileLogging { get; set; }
}

/// <summary>
/// Features configuration
/// </summary>
public class FeaturesConfig
{
	public bool ChatEnabled { get; set; }
	public bool WeatherEnabled { get; set; }
	public bool GrpcEnabled { get; set; }
	public bool WebSocketEnabled { get; set; }
}

/// <summary>
/// System information
/// </summary>
public class SystemInfo
{
	public string Platform { get; set; } = string.Empty;
	public string OsVersion { get; set; } = string.Empty;
	public string DotnetVersion { get; set; } = string.Empty;
	public string WorkingDirectory { get; set; } = string.Empty;
	public string MachineName { get; set; } = string.Empty;
	public int ProcessorCount { get; set; }
	public long TotalMemory { get; set; }
}

/// <summary>
/// Configuration update response
/// </summary>
public class ConfigUpdateResponse
{
	public bool Success { get; set; }
	public string Message { get; set; } = string.Empty;
	public string[] ReceivedKeys { get; set; } = Array.Empty<string>();
	public string Note { get; set; } = string.Empty;
}
