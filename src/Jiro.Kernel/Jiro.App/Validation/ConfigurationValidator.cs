using Jiro.Core.Options;

using Microsoft.Extensions.Configuration;

namespace Jiro.App.Validation;

/// <summary>
/// Provides comprehensive validation for Jiro application configuration.
/// Validates all required settings before app startup and provides clear error messages.
/// </summary>
public static class ConfigurationValidator
{
	/// <summary>
	/// Validates all required configuration settings and returns detailed error messages.
	/// In test mode, also sets default values for missing configurations.
	/// </summary>
	/// <param name="configuration">The configuration manager to validate</param>
	/// <param name="isTestMode">Whether the application is running in test mode</param>
	/// <returns>A list of validation errors. Empty list indicates valid configuration.</returns>
	public static List<string> ValidateSettings(IConfiguration configuration, bool isTestMode = false)
	{
		var errors = new List<string>();

		// In test mode, set default values first
		if (isTestMode)
		{
			SetTestModeDefaults(configuration);
		}

		// Validate core application settings
		errors.AddRange(ValidateApplicationOptions(configuration, isTestMode));

		// Validate JiroCloud settings (required for communication)
		errors.AddRange(ValidateJiroCloudOptions(configuration, isTestMode));

		// Validate database connection
		errors.AddRange(ValidateDatabaseConnection(configuration, isTestMode));


		// Validate Chat settings (if chat is enabled)
		errors.AddRange(ValidateChatOptions(configuration, isTestMode));

		return errors;
	}

	/// <summary>
	/// Sets default values for test mode to ensure the application can start.
	/// </summary>
	private static void SetTestModeDefaults(IConfiguration configuration)
	{
		if (configuration is ConfigurationManager configManager)
		{
			// Set default connection string if missing
			if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("JiroContext")))
			{
				configManager["ConnectionStrings:JiroContext"] = "Data Source=:memory:";
			}

			// Set default gRPC server URL if missing (use old path for backward compatibility)
			if (string.IsNullOrWhiteSpace(configuration.GetSection("JiroCloud:Grpc:ServerUrl").Value) &&
				string.IsNullOrWhiteSpace(configuration.GetSection("Grpc:ServerUrl").Value))
			{
				configManager["JiroCloud:Grpc:ServerUrl"] = "https://localhost:5001";
			}

			// Set default WebSocket API key if missing (use old path for backward compatibility)
			if (string.IsNullOrWhiteSpace(configuration.GetSection("JiroCloud:ApiKey").Value) &&
				string.IsNullOrWhiteSpace(configuration.GetSection("WebSocket:ApiKey").Value))
			{
				configManager["JiroCloud:ApiKey"] = "test-jirocloud-api-key";
			}

			// Set default WebSocket Hub URL if missing (use old path for backward compatibility)
			if (string.IsNullOrWhiteSpace(configuration.GetSection("JiroCloud:WebSocket:HubUrl").Value) &&
				string.IsNullOrWhiteSpace(configuration.GetSection("WebSocket:HubUrl").Value))
			{
				configManager["JiroCloud:WebSocket:HubUrl"] = "https://localhost:5001/instanceHub";
			}
		}
	}

	/// <summary>
	/// Validates core application configuration options.
	/// </summary>
	private static List<string> ValidateApplicationOptions(IConfiguration configuration, bool isTestMode)
	{
		var errors = new List<string>();
		var appOptions = new ApplicationOptions();
		configuration.Bind(appOptions);

		if (!isTestMode)
		{
			if (string.IsNullOrWhiteSpace(appOptions.ApiKey))
			{
				errors.Add("❌ ApiKey is required. Set it in appsettings.json or use JIRO_ApiKey environment variable.");
			}

			if (string.IsNullOrWhiteSpace(appOptions.JiroApi))
			{
				errors.Add("❌ JiroApi is required. Set it in appsettings.json or use JIRO_JiroApi environment variable.");
			}
			else if (!Uri.TryCreate(appOptions.JiroApi, UriKind.Absolute, out var jiroApiUri))
			{
				errors.Add("❌ JiroApi must be a valid URL. Current value: " + appOptions.JiroApi);
			}
		}

		if (!string.IsNullOrWhiteSpace(appOptions.TokenizerUrl) &&
			!Uri.TryCreate(appOptions.TokenizerUrl, UriKind.Absolute, out var tokenizerUri))
		{
			errors.Add("❌ TokenizerUrl must be a valid URL if provided. Current value: " + appOptions.TokenizerUrl);
		}

		return errors;
	}

	/// <summary>
	/// Validates JiroCloud configuration options.
	/// </summary>
	private static List<string> ValidateJiroCloudOptions(IConfiguration configuration, bool isTestMode)
	{
		var errors = new List<string>();
		var jiroCloudOptions = new JiroCloudOptions();
		configuration.GetSection(JiroCloudOptions.JiroCloud).Bind(jiroCloudOptions);

		if (!isTestMode)
		{
			// Validate JiroCloud API key
			if (string.IsNullOrWhiteSpace(jiroCloudOptions.ApiKey) ||
				jiroCloudOptions.ApiKey == "your-jirocloud-api-key-here")
			{
				errors.Add("❌ JiroCloud:ApiKey is required. Set it in appsettings.json or use JIRO_JiroCloud__ApiKey environment variable.");
			}

			// Validate WebSocket configuration
			if (string.IsNullOrWhiteSpace(jiroCloudOptions.WebSocket.HubUrl))
			{
				errors.Add("❌ JiroCloud:WebSocket:HubUrl is required. Set it in appsettings.json or use JIRO_JiroCloud__WebSocket__HubUrl environment variable.");
			}
			else if (!Uri.TryCreate(jiroCloudOptions.WebSocket.HubUrl, UriKind.Absolute, out var hubUri))
			{
				errors.Add("❌ JiroCloud:WebSocket:HubUrl must be a valid URL. Current value: " + jiroCloudOptions.WebSocket.HubUrl);
			}

			// Validate gRPC configuration
			if (string.IsNullOrWhiteSpace(jiroCloudOptions.Grpc.ServerUrl))
			{
				errors.Add("❌ JiroCloud:Grpc:ServerUrl is required. Set it in appsettings.json or use JIRO_JiroCloud__Grpc__ServerUrl environment variable.");
			}
			else if (!Uri.TryCreate(jiroCloudOptions.Grpc.ServerUrl, UriKind.Absolute, out var grpcUri))
			{
				errors.Add("❌ JiroCloud:Grpc:ServerUrl must be a valid URL. Current value: " + jiroCloudOptions.Grpc.ServerUrl);
			}
		}

		// Validate timeout values (even in test mode)
		if (jiroCloudOptions.Grpc.TimeoutMs <= 0)
		{
			errors.Add("❌ JiroCloud:Grpc:TimeoutMs must be greater than 0. Current value: " + jiroCloudOptions.Grpc.TimeoutMs);
		}

		if (jiroCloudOptions.WebSocket.HandshakeTimeoutMs <= 0)
		{
			errors.Add("❌ JiroCloud:WebSocket:HandshakeTimeoutMs must be greater than 0. Current value: " + jiroCloudOptions.WebSocket.HandshakeTimeoutMs);
		}

		if (jiroCloudOptions.WebSocket.ReconnectionAttempts < 0)
		{
			errors.Add("❌ JiroCloud:WebSocket:ReconnectionAttempts must be 0 or greater. Current value: " + jiroCloudOptions.WebSocket.ReconnectionAttempts);
		}

		return errors;
	}

	/// <summary>
	/// Validates database connection configuration.
	/// </summary>
	private static List<string> ValidateDatabaseConnection(IConfiguration configuration, bool isTestMode)
	{
		var errors = new List<string>();

		if (!isTestMode)
		{
			var connectionString = configuration.GetConnectionString("JiroContext");
			if (string.IsNullOrWhiteSpace(connectionString))
			{
				errors.Add("❌ ConnectionStrings:JiroContext is required. Set it in appsettings.json or use JIRO_ConnectionStrings__JiroContext environment variable.");
			}
		}

		return errors;
	}


	/// <summary>
	/// Validates Chat configuration options when chat is enabled.
	/// </summary>
	private static List<string> ValidateChatOptions(IConfiguration configuration, bool isTestMode)
	{
		var errors = new List<string>();
		var chatOptions = new ChatOptions();
		configuration.GetSection(ChatOptions.Chat).Bind(chatOptions);

		// Only validate chat options if chat is enabled
		if (chatOptions.Enabled && !isTestMode)
		{
			if (string.IsNullOrWhiteSpace(chatOptions.AuthToken))
			{
				errors.Add("❌ Chat:AuthToken is required when Chat:Enabled is true. Set it in appsettings.json or use JIRO_Chat__AuthToken environment variable.");
			}

			if (chatOptions.TokenLimit <= 0)
			{
				errors.Add("❌ Chat:TokenLimit must be greater than 0 when chat is enabled. Current value: " + chatOptions.TokenLimit);
			}

			if (string.IsNullOrWhiteSpace(chatOptions.SystemMessage))
			{
				errors.Add("⚠️  Chat:SystemMessage is empty. Consider setting a system message for better AI responses.");
			}
		}

		return errors;
	}

	/// <summary>
	/// Prints configuration validation results to the console with color coding.
	/// </summary>
	/// <param name="errors">List of validation errors</param>
	/// <param name="isTestMode">Whether the application is running in test mode</param>
	public static void PrintValidationResults(List<string> errors, bool isTestMode = false)
	{
		if (errors.Count == 0)
		{
			Console.WriteLine("✅ Configuration validation passed!");
			if (isTestMode)
			{
				Console.WriteLine("🧪 Running in test mode - some validations were skipped");
			}
		}
		else
		{
			Console.WriteLine("❌ Configuration validation failed!");
			Console.WriteLine($"Found {errors.Count} configuration error(s):");
			Console.WriteLine();

			foreach (var error in errors)
			{
				Console.WriteLine($"  {error}");
			}

			Console.WriteLine();
			Console.WriteLine("💡 Tips:");
			Console.WriteLine("  • Copy appsettings.example.json to appsettings.json and configure your values");
			Console.WriteLine("  • Use environment variables with JIRO_ prefix (e.g., JIRO_ApiKey=your-key)");
			Console.WriteLine("  • Use double underscores for nested settings (e.g., JIRO_JiroCloud__ApiKey=your-key)");
			Console.WriteLine("  • Run the setup script: ./scripts/setup-project.ps1 (Windows) or ./scripts/setup-project.sh (Linux/macOS)");
		}
	}
}
