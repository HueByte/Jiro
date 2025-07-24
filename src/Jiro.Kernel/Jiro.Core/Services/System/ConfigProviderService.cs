using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

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
				InstanceId = _configuration.GetValue<string>("InstanceId") ?? Environment.MachineName,
				Configuration = new Shared.Websocket.Requests.ConfigurationSection
				{
					Values = GetJiroRelatedConfiguration()
				},
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
	public async Task<ConfigUpdateResponse> UpdateConfigAsync(string configJson)
	{
		try
		{
			_logger.LogInformation("Received configuration update request");

			if (string.IsNullOrEmpty(configJson))
			{
				throw new ArgumentException("Configuration data is required", nameof(configJson));
			}

			// Parse the configuration data
			var configData = JsonSerializer.Deserialize<Dictionary<string, object>>(configJson);
			if (configData == null)
			{
				throw new ArgumentException("Invalid configuration JSON format", nameof(configJson));
			}

			// Define paths to configuration files
			var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
			var appSettingsExamplePath = Path.Combine(AppContext.BaseDirectory, "appsettings.example.json");

			// Backup current configuration
			var backupPath = $"{appSettingsPath}.backup.{DateTime.UtcNow:yyyyMMddHHmmss}";
			if (File.Exists(appSettingsPath))
			{
				File.Copy(appSettingsPath, backupPath);
				_logger.LogInformation("Created backup of current configuration at: {BackupPath}", backupPath);
			}

			try
			{
				// Load current configuration
				var currentConfig = await LoadConfigurationFromFileAsync(appSettingsPath);

				// Update only the changed values
				var updatedConfig = UpdateConfigurationValues(currentConfig, configData);
				var updatedKeys = GetChangedKeys(currentConfig, updatedConfig);

				// Validate the updated configuration
				if (!ValidateConfiguration(updatedConfig))
				{
					_logger.LogWarning("Configuration validation failed. Restoring from example configuration.");

					// Copy from example configuration if validation fails
					if (File.Exists(appSettingsExamplePath))
					{
						updatedConfig = await LoadConfigurationFromFileAsync(appSettingsExamplePath);
						_logger.LogInformation("Restored configuration from example file");
					}
					else
					{
						throw new InvalidOperationException("Example configuration file not found and validation failed");
					}
				}

				// Save the updated configuration
				await SaveConfigurationToFileAsync(appSettingsPath, updatedConfig);
				_logger.LogInformation("Configuration updated successfully. Changed keys: {UpdatedKeys}", string.Join(", ", updatedKeys));

				var response = new ConfigUpdateResponse
				{
					Success = true,
					Message = $"Configuration updated successfully. {updatedKeys.Length} value(s) changed.",
					ReceivedKeys = configData.Keys.ToArray(),
					Note = updatedKeys.Length == 0 ? "No changes were made to the configuration." :
						   $"Updated: {string.Join(", ", updatedKeys)}. Application restart may be required for some changes to take effect."
				};

				return response;
			}
			catch (Exception)
			{
				// Restore backup if something went wrong
				if (File.Exists(backupPath))
				{
					File.Copy(backupPath, appSettingsPath, overwrite: true);
					_logger.LogInformation("Restored configuration from backup due to update failure");
				}
				throw;
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating configuration");

			var errorResponse = new ConfigUpdateResponse
			{
				Success = false,
				Message = $"Configuration update failed: {ex.Message}",
				ReceivedKeys = Array.Empty<string>(),
				Note = "The configuration was not modified due to an error."
			};

			return errorResponse;
		}
	}

	private async Task<Dictionary<string, object>> LoadConfigurationFromFileAsync(string filePath)
	{
		if (!File.Exists(filePath))
		{
			return new Dictionary<string, object>();
		}

		var configText = await File.ReadAllTextAsync(filePath);
		var config = JsonSerializer.Deserialize<Dictionary<string, object>>(configText);
		return config ?? new Dictionary<string, object>();
	}

	private Dictionary<string, object> UpdateConfigurationValues(Dictionary<string, object> currentConfig, Dictionary<string, object> updates)
	{
		var updatedConfig = new Dictionary<string, object>(currentConfig);

		foreach (var kvp in updates)
		{
			// Use dot notation for nested properties (e.g., "Gpt.AuthToken")
			SetNestedValue(updatedConfig, kvp.Key, kvp.Value);
		}

		return updatedConfig;
	}

	private void SetNestedValue(Dictionary<string, object> config, string path, object value)
	{
		var keys = path.Split('.');
		var current = config;

		for (int i = 0; i < keys.Length - 1; i++)
		{
			var key = keys[i];
			if (!current.ContainsKey(key))
			{
				current[key] = new Dictionary<string, object>();
			}

			if (current[key] is JsonElement jsonElement)
			{
				current[key] = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonElement.GetRawText())
					?? new Dictionary<string, object>();
			}

			if (current[key] is Dictionary<string, object> dict)
			{
				current = dict;
			}
			else
			{
				// If the intermediate value is not a dictionary, create a new one
				current[key] = new Dictionary<string, object>();
				current = (Dictionary<string, object>)current[key];
			}
		}

		current[keys[^1]] = value;
	}

	private string[] GetChangedKeys(Dictionary<string, object> original, Dictionary<string, object> updated)
	{
		var changedKeys = new List<string>();
		CompareConfigurations(original, updated, "", changedKeys);
		return changedKeys.ToArray();
	}

	private void CompareConfigurations(Dictionary<string, object> original, Dictionary<string, object> updated, string prefix, List<string> changedKeys)
	{
		foreach (var kvp in updated)
		{
			var fullKey = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}.{kvp.Key}";

			if (!original.ContainsKey(kvp.Key))
			{
				changedKeys.Add(fullKey);
				continue;
			}

			var originalValue = original[kvp.Key];
			var updatedValue = kvp.Value;

			if (originalValue is Dictionary<string, object> originalDict &&
				updatedValue is Dictionary<string, object> updatedDict)
			{
				CompareConfigurations(originalDict, updatedDict, fullKey, changedKeys);
			}
			else if (!Equals(originalValue, updatedValue))
			{
				changedKeys.Add(fullKey);
			}
		}
	}

	private bool ValidateConfiguration(Dictionary<string, object> config)
	{
		try
		{
			// Basic validation - ensure required sections exist
			var requiredSections = new[] { "Serilog", "WebSocket", "Grpc" };

			foreach (var section in requiredSections)
			{
				if (!config.ContainsKey(section))
				{
					_logger.LogWarning("Required configuration section missing: {Section}", section);
					return false;
				}
			}

			// Validate core application settings (can be overridden by JIRO_ env vars)
			if (!config.ContainsKey("ApiKey") || string.IsNullOrWhiteSpace(config["ApiKey"]?.ToString()))
			{
				_logger.LogWarning("ApiKey is missing or empty. Set it in configuration or use JIRO_ApiKey environment variable");
				return false;
			}

			if (!config.ContainsKey("JiroApi") || string.IsNullOrWhiteSpace(config["JiroApi"]?.ToString()))
			{
				_logger.LogWarning("JiroApi is missing or empty. Set it in configuration or use JIRO_JiroApi environment variable");
				return false;
			}

			// Validate specific configuration values
			if (config.TryGetValue("WebSocket", out var webSocketSection) &&
				webSocketSection is Dictionary<string, object> wsConfig)
			{
				if (!wsConfig.ContainsKey("HubUrl") ||
					string.IsNullOrWhiteSpace(wsConfig["HubUrl"]?.ToString()))
				{
					_logger.LogWarning("WebSocket.HubUrl is missing or empty");
					return false;
				}
			}

			if (config.TryGetValue("Grpc", out var grpcSection) &&
				grpcSection is Dictionary<string, object> grpcConfig)
			{
				// Note: Grpc.ServerUrl is optional as it may default to JiroApi
				// Only validate if explicitly set
				if (grpcConfig.ContainsKey("ServerUrl") &&
					string.IsNullOrWhiteSpace(grpcConfig["ServerUrl"]?.ToString()))
				{
					_logger.LogWarning("Grpc.ServerUrl is set but empty");
					return false;
				}
			}

			// Validate DataPaths if present (optional section with defaults)
			if (config.TryGetValue("DataPaths", out var dataPathsSection) &&
				dataPathsSection is Dictionary<string, object> dpConfig)
			{
				var pathKeys = new[] { "Logs", "Themes", "Plugins", "Database" };
				foreach (var key in pathKeys)
				{
					if (dpConfig.ContainsKey(key) &&
						string.IsNullOrWhiteSpace(dpConfig[key]?.ToString()))
					{
						_logger.LogWarning("DataPaths.{Key} is set but empty", key);
						return false;
					}
				}
			}

			// Try to serialize back to JSON to ensure it's valid
			JsonSerializer.Serialize(config);

			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Configuration validation failed");
			return false;
		}
	}

	private Dictionary<string, object> GetJiroRelatedConfiguration()
	{
		// With JIRO_ prefix configuration, all JIRO_ prefixed env vars are automatically available
		// as regular configuration keys (prefix is stripped by IConfiguration)
		var jiroRelatedKeys = new[]
		{
			"ApiKey",          // Core API key configuration
			"JiroApi",         // Jiro API URL
			"TokenizerUrl",    // Tokenizer service URL
			"Whitelist",       // Whitelist configuration
			"DataPaths",       // Data storage paths configuration
			"Chat",            // Chat/AI configuration
			"Gpt",             // Legacy OpenAI/GPT configuration
			"JWT",             // JWT authentication
			"Serilog",         // Logging configuration
			"WebSocket",       // WebSocket configuration
			"Grpc",            // gRPC configuration
			"ConnectionStrings", // Database connections
			"Modules",         // Plugin modules
			"Log"              // Log configuration
		};

		var filteredConfig = new Dictionary<string, object>();

		// Get all configuration entries
		var allConfig = _configuration.AsEnumerable();

		foreach (var kvp in allConfig)
		{
			if (string.IsNullOrEmpty(kvp.Key) || kvp.Value == null)
				continue;

			// Check if the key matches any Jiro-related configuration section
			bool isJiroRelated = jiroRelatedKeys.Any(key =>
				kvp.Key.Equals(key, StringComparison.OrdinalIgnoreCase) ||
				kvp.Key.StartsWith($"{key}:", StringComparison.OrdinalIgnoreCase));

			// Include ASP.NET Core and .NET runtime environment info
			if (!isJiroRelated)
			{
				var environmentPrefixes = new[] { "ASPNETCORE_", "DOTNET_" };
				isJiroRelated = environmentPrefixes.Any(prefix =>
					kvp.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
			}

			if (isJiroRelated)
			{
				filteredConfig[kvp.Key] = kvp.Value;
			}
		}

		// Add a note about JIRO_ prefix support
		filteredConfig["_ConfigurationNote"] = "Environment variables with JIRO_ prefix automatically override corresponding configuration values";

		return filteredConfig;
	}

	private async Task SaveConfigurationToFileAsync(string filePath, Dictionary<string, object> config)
	{
		var options = new JsonSerializerOptions
		{
			WriteIndented = true,
			PropertyNamingPolicy = null
		};

		var configJson = JsonSerializer.Serialize(config, options);
		await File.WriteAllTextAsync(filePath, configJson);
	}
}
