namespace Jiro.Core.Options;

/// <summary>
/// Configuration options for logging settings including log levels for different application components.
/// </summary>
public class LogOptions : IOption
{
	/// <summary>
	/// The configuration section name for log options.
	/// </summary>
	public const string Log = "Log";

	/// <summary>
	/// Gets or sets the default log level for the application.
	/// </summary>
	public string LogLevel { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the time interval configuration for log operations.
	/// </summary>
	public string TimeInterval { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the log level for ASP.NET Core framework components.
	/// </summary>
	public string AspNetCoreLevel { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the log level for database operations and Entity Framework.
	/// </summary>
	public string DatabaseLevel { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the log level for system-level operations and services.
	/// </summary>
	public string SystemLevel { get; set; } = string.Empty;
}
