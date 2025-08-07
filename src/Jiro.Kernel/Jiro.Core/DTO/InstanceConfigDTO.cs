using System.Text.Json.Serialization;

namespace Jiro.Core.DTO;

/// <summary>
/// Data transfer object representing the complete configuration for a Jiro instance.
/// </summary>
public class InstanceConfigDTO
{
	/// <summary>
	/// Gets or sets the URLs configuration for the application hosting.
	/// </summary>
	[JsonPropertyName("urls")]
	public string? urls
	{
		get; set;
	}


	/// <summary>
	/// Gets or sets the database connection strings configuration.
	/// </summary>
	[JsonPropertyName("ConnectionStrings")]
	public ConnectionStrings? ConnectionStrings
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the logging configuration settings.
	/// </summary>
	[JsonPropertyName("Log")]
	public Log? Log
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets a value indicating whether whitelist functionality is enabled.
	/// </summary>
	[JsonPropertyName("Whitelist")]
	public bool? Whitelist
	{
		get; set;
	}


	/// <summary>
	/// Gets or sets the GPT AI service configuration settings.
	/// </summary>
	[JsonPropertyName("Gpt")]
	public Gpt? Gpt
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the allowed hosts configuration for CORS and security.
	/// </summary>
	[JsonPropertyName("AllowedHosts")]
	public string? AllowedHosts
	{
		get; set;
	}
}

/// <summary>
/// Configuration settings specific to ChatGPT functionality.
/// </summary>
public class ChatGpt
{
	/// <summary>
	/// Gets or sets the system message that defines the AI assistant's personality and behavior.
	/// </summary>
	[JsonPropertyName("SystemMessage")]
	public string? SystemMessage
	{
		get; set;
	}
}

/// <summary>
/// Database connection strings configuration.
/// </summary>
public class ConnectionStrings
{
	/// <summary>
	/// Gets or sets the connection string for the main Jiro database context.
	/// </summary>
	[JsonPropertyName("JiroContext")]
	public string? JiroContext
	{
		get; set;
	}
}

/// <summary>
/// Configuration settings for GPT AI service integration.
/// </summary>
public class Gpt
{
	/// <summary>
	/// Gets or sets a value indicating whether GPT functionality is enabled.
	/// </summary>
	[JsonPropertyName("Enable")]
	public bool? Enable
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the base URL for the GPT API service.
	/// </summary>
	[JsonPropertyName("BaseUrl")]
	public string? BaseUrl
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the authentication token for accessing the GPT API.
	/// </summary>
	[JsonPropertyName("AuthToken")]
	public string? AuthToken
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the organization identifier for the GPT API.
	/// </summary>
	[JsonPropertyName("Organization")]
	public string? Organization
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets a value indicating whether fine-tuning features are enabled.
	/// </summary>
	[JsonPropertyName("FineTune")]
	public bool? FineTune
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets a value indicating whether to use ChatGPT instead of legacy GPT models.
	/// </summary>
	[JsonPropertyName("UseChatGpt")]
	public bool? UseChatGpt
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the ChatGPT-specific configuration settings.
	/// </summary>
	[JsonPropertyName("ChatGpt")]
	public ChatGpt? ChatGpt
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the single GPT model configuration settings.
	/// </summary>
	[JsonPropertyName("SingleGpt")]
	public SingleGpt? SingleGpt
	{
		get; set;
	}
}


/// <summary>
/// Logging configuration settings for the application.
/// </summary>
public class Log
{
	/// <summary>
	/// Gets or sets the time interval for log rotation or aggregation.
	/// </summary>
	[JsonPropertyName("TimeInterval")]
	public string? TimeInterval
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the default log level for the application.
	/// </summary>
	[JsonPropertyName("LogLevel")]
	public string? LogLevel
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the log level specifically for ASP.NET Core framework logs.
	/// </summary>
	[JsonPropertyName("AspNetCoreLevel")]
	public string? AspNetCoreLevel
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the log level for database-related operations.
	/// </summary>
	[JsonPropertyName("DatabaseLevel")]
	public string? DatabaseLevel
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the log level for system-level operations and services.
	/// </summary>
	[JsonPropertyName("SystemLevel")]
	public string? SystemLevel
	{
		get; set;
	}
}

/// <summary>
/// Configuration settings for single GPT model operations.
/// </summary>
public class SingleGpt
{
	/// <summary>
	/// Gets or sets the maximum number of tokens allowed per request.
	/// </summary>
	[JsonPropertyName("TokenLimit")]
	public int? TokenLimit
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the context message that provides background information to the AI model.
	/// </summary>
	[JsonPropertyName("ContextMessage")]
	public string? ContextMessage
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the stop sequences that signal the model to stop generating text.
	/// </summary>
	[JsonPropertyName("Stop")]
	public string? Stop
	{
		get; set;
	}

	/// <summary>
	/// Gets or sets the specific GPT model identifier to use for requests.
	/// </summary>
	[JsonPropertyName("Model")]
	public string? Model
	{
		get; set;
	}
}
