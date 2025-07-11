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
	/// Creates default application folders (logs, save, modules) if they don't exist.
	/// </summary>
	/// <returns>The current <see cref="EnvironmentConfigurator"/> instance for method chaining.</returns>
	public EnvironmentConfigurator PrepareDefaultFolders()
	{
		if (!Directory.Exists(Path.Join(AppContext.BaseDirectory, "logs")))
			Directory.CreateDirectory(Path.Join(AppContext.BaseDirectory, "logs"));

		if (!Directory.Exists(Path.Join(AppContext.BaseDirectory, "save")))
			Directory.CreateDirectory(Path.Join(AppContext.BaseDirectory, "save"));

		if (!Directory.Exists(Path.Join(AppContext.BaseDirectory, "modules")))
			Directory.CreateDirectory(Path.Join(AppContext.BaseDirectory, "modules"));

		return this;
	}

	/// <summary>
	/// Prepares the logs folder based on the API_LOGS_PATH configuration value, creating directories as needed.
	/// </summary>
	/// <returns>The current <see cref="EnvironmentConfigurator"/> instance for method chaining.</returns>
	/// <exception cref="Exception">Thrown when the API_LOGS_PATH configuration points to an invalid directory.</exception>
	public EnvironmentConfigurator PrepareLogsFolder()
	{
		var logsPath = _config.GetValue<string>("API_LOGS_PATH");
		if (string.IsNullOrEmpty(logsPath))
			return this;

		var logsInfo = new FileInfo(logsPath);
		var rootDirectory = logsInfo?.Directory?.Parent?.FullName;
		var logsDirectory = logsInfo?.Directory?.FullName;

		if (!Directory.Exists(rootDirectory))
			throw new Exception($"Invalid path for API_LOGS_PATH\nRoot Directory: {rootDirectory}\nPath: {logsPath}");

		if (!string.IsNullOrEmpty(logsDirectory) && !Directory.Exists(logsDirectory))
		{
			Directory.CreateDirectory(logsDirectory);
		}

		return this;
	}

	/// <summary>
	/// Prepares and loads configuration files, handling custom CONFIG_PATH settings and creating default configuration files as needed.
	/// </summary>
	/// <returns>The current <see cref="EnvironmentConfigurator"/> instance for method chaining.</returns>
	/// <exception cref="Exception">Thrown when configuration file loading fails or when CONFIG_PATH points to an invalid location.</exception>
	public EnvironmentConfigurator PrepareConfigFiles()
	{
		string finalPath = "";
		try
		{
			var envConfigPath = _config.GetValue<string>("CONFIG_PATH");

			// if user provides custom path for config
			if (!string.IsNullOrEmpty(envConfigPath))
			{
				// if path meets requirements, use it
				if (File.Exists(envConfigPath) && Path.GetExtension(envConfigPath) == ".json")
				{
					finalPath = envConfigPath;
				}
				else
				{
					// check if directory exists and throw if doesn't
					var containingDirectory = new FileInfo(envConfigPath).Directory?.FullName;
					if (!Directory.Exists(containingDirectory))
						throw new Exception($"Invalid path for CONFIG_PATH\nDirectory: {containingDirectory}\nPath: {envConfigPath}");

					// if directory exists, copy example config to it, and then use it
					finalPath = Path.Join(containingDirectory, "appsettings.json");
					File.Copy(Path.Join(AppContext.BaseDirectory, "appsettings.example.json"), finalPath);
				}
			}
			else
			{
				if (!File.Exists(Path.Join(AppContext.BaseDirectory, "appsettings.json")))
					File.Copy(Path.Join(AppContext.BaseDirectory, "appsettings.example.json"), Path.Join(AppContext.BaseDirectory, "appsettings.json"));

				finalPath = Path.Join(AppContext.BaseDirectory, "appsettings.json");
			}


			_config.AddJsonFile(finalPath, optional: false, reloadOnChange: false);
		}
		catch (Exception ex)
		{
			throw new Exception($"Error while loading config file: {finalPath}", ex);
		}

		return this;
	}
}
