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
