using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

using Jiro.Core.Services.System.Models;
using Jiro.Shared.Websocket.Responses;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jiro.Core.Services.System;

/// <inheritdoc/>
public class ConfigProviderService : IConfigProviderService
{
	private readonly ILogger<ConfigProviderService> _logger;
	private readonly IConfiguration _configuration;

	/// <summary>
	/// Initializes a new instance of the <see cref="ConfigProviderService"/> class.
	/// </summary>
	/// <param name="logger">Logger instance for logging.</param>
	/// <param name="configuration">Configuration instance for accessing system settings.</param>
	public ConfigProviderService(ILogger<ConfigProviderService> logger, IConfiguration configuration)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
	}

	/// <inheritdoc/>
	public Task<ConfigResponse> GetConfigAsync()
	{
		try
		{
			_logger.LogInformation("Getting system configuration");

			var config = new ConfigResponse
			{
				ApplicationName = "Jiro",
				Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
				Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
				InstanceId = _configuration.GetValue<string>("INSTANCE_ID") ?? Environment.MachineName,
				Configuration = new Shared.Websocket.Requests.ConfigurationSection
				{
					Values = _configuration.AsEnumerable().ToDictionary(
						static kvp => kvp.Key,
						static kvp => (object)(kvp.Value ?? string.Empty)
					)
				},
				// {
				// 	Chat = _configuration.GetSection("Chat").Get<object>(),
				// 	Logging = new LoggingConfig
				// 	{
				// 		LogLevel = _configuration.GetValue<string>("Logging:LogLevel:Default"),
				// 		EnableConsoleLogging = true,
				// 		EnableFileLogging = true
				// 	},
				// 	Features = new FeaturesConfig
				// 	{
				// 		ChatEnabled = _configuration.GetValue<bool>("Chat:Enabled"),
				// 		WeatherEnabled = true,
				// 		GrpcEnabled = _configuration.GetSection("Grpc").Exists(),
				// 		WebSocketEnabled = _configuration.GetSection("WebSocket").Exists()
				// 	}
				// },
				SystemInfo = new Shared.Websocket.Requests.SystemInfo
				{
					OperatingSystem = Environment.OSVersion.Platform.ToString(),
					RuntimeVersion = Environment.Version.ToString(),
					MachineName = Environment.MachineName,
					ProcessorCount = Environment.ProcessorCount,
					TotalMemory = GC.GetTotalMemory(false),
				},
				Uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()
			};

			return Task.FromResult(config);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving configuration");
			throw;
		}
	}

	/// <inheritdoc/>
	public Task<ConfigUpdateResponse> UpdateConfigAsync(string configJson)
	{
		try
		{
			// TODO: Implement actual configuration update logic
			_logger.LogInformation("Received configuration update request");

			if (string.IsNullOrEmpty(configJson))
			{
				throw new ArgumentException("Configuration data is required", nameof(configJson));
			}

			// Try to parse the configuration
			var configData = JsonSerializer.Deserialize<Dictionary<string, object>>(configJson);

			var response = new ConfigUpdateResponse
			{
				Success = true,
				Message = "Configuration update received (read-only mode)",
				ReceivedKeys = configData?.Keys.ToArray() ?? Array.Empty<string>(),
				Note = "This is a read-only implementation for security. Configuration changes require application restart."
			};

			return Task.FromResult(response);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating configuration");
			throw;
		}
	}
}
