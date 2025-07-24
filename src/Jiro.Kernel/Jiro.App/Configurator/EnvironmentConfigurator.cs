using Jiro.Core.Options;

using Microsoft.Extensions.Configuration;

namespace Jiro.App.Configurator;

/// <summary>
/// Provides configuration functionality for environment setup, including folder creation and configuration file management.
/// </summary>
public class EnvironmentConfigurator
{
	private readonly ConfigurationManager _config;

	/// <summary>
	/// Initializes a new instance of the <see cref="EnvironmentConfigurator"/> class.
	/// </summary>
	/// <param name="config">The configuration manager used for environment setup.</param>
	public EnvironmentConfigurator(ConfigurationManager config)
	{
		_config = config;
	}

	/// <summary>
	/// Creates default application folders if they don't exist.
	/// Uses configurable paths from DataPaths section or falls back to defaults.
	/// JIRO_ prefixed environment variables automatically override these configuration values.
	/// </summary>
	/// <returns>The current <see cref="EnvironmentConfigurator"/> instance for method chaining.</returns>
	public EnvironmentConfigurator PrepareDefaultFolders()
	{
		// Get data paths options from configuration
		var dataPathsOptions = new DataPathsOptions();
		_config.GetSection(DataPathsOptions.DataPaths).Bind(dataPathsOptions);
		
		// Create directories using options
		if (!Directory.Exists(dataPathsOptions.AbsoluteLogsPath))
			Directory.CreateDirectory(dataPathsOptions.AbsoluteLogsPath);

		if (!Directory.Exists(dataPathsOptions.AbsoluteThemesPath))
			Directory.CreateDirectory(dataPathsOptions.AbsoluteThemesPath);

		if (!Directory.Exists(dataPathsOptions.AbsolutePluginsPath))
			Directory.CreateDirectory(dataPathsOptions.AbsolutePluginsPath);

		var databaseDir = Path.GetDirectoryName(dataPathsOptions.AbsoluteDatabasePath);
		if (!string.IsNullOrEmpty(databaseDir) && !Directory.Exists(databaseDir))
			Directory.CreateDirectory(databaseDir);

		// Legacy folders for backward compatibility
		if (!Directory.Exists(Path.Join(AppContext.BaseDirectory, "logs")))
			Directory.CreateDirectory(Path.Join(AppContext.BaseDirectory, "logs"));

		if (!Directory.Exists(Path.Join(AppContext.BaseDirectory, "save")))
			Directory.CreateDirectory(Path.Join(AppContext.BaseDirectory, "save"));

		if (!Directory.Exists(Path.Join(AppContext.BaseDirectory, "modules")))
			Directory.CreateDirectory(Path.Join(AppContext.BaseDirectory, "modules"));

		return this;
	}

	/// <summary>
	/// Prepares the logs folder based on the Serilog configuration, creating directories as needed.
	/// </summary>
	/// <returns>The current <see cref="EnvironmentConfigurator"/> instance for method chaining.</returns>
	public EnvironmentConfigurator PrepareLogsFolder()
	{
		// Extract log file paths from Serilog configuration
		var serilogWriteTo = _config.GetSection("Serilog:WriteTo").GetChildren();

		foreach (var sink in serilogWriteTo)
		{
			var sinkName = sink.GetValue<string>("Name");
			if (sinkName == "File")
			{
				var filePath = sink.GetValue<string>("Args:path");
				if (!string.IsNullOrEmpty(filePath))
				{
					var directory = Path.GetDirectoryName(filePath);
					if (!string.IsNullOrEmpty(directory))
					{
						var fullPath = Path.IsPathRooted(directory)
							? directory
							: Path.Combine(AppContext.BaseDirectory, directory);

						if (!Directory.Exists(fullPath))
						{
							Directory.CreateDirectory(fullPath);
						}
					}
				}
			}
		}

		return this;
	}

}
