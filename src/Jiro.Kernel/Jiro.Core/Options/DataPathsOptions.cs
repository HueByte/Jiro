namespace Jiro.Core.Options;

/// <summary>
/// Configuration options for data storage paths used throughout the Jiro application.
/// These paths can be overridden using JIRO_ prefixed environment variables.
/// </summary>
public class DataPathsOptions : IOption
{
	/// <summary>
	/// Configuration section name for data paths.
	/// </summary>
	public const string DataPaths = "DataPaths";

	/// <summary>
	/// Gets or sets the path for storing log files.
	/// Can be overridden with JIRO_DataPaths__Logs environment variable.
	/// </summary>
	public string Logs { get; set; } = "Data/Logs";

	/// <summary>
	/// Gets or sets the path for storing theme files.
	/// Can be overridden with JIRO_DataPaths__Themes environment variable.
	/// </summary>
	public string Themes { get; set; } = "Data/Themes";

	/// <summary>
	/// Gets or sets the path for storing plugin/module files.
	/// Can be overridden with JIRO_DataPaths__Plugins environment variable.
	/// </summary>
	public string Plugins { get; set; } = "Data/Plugins";

	/// <summary>
	/// Gets or sets the path for the SQLite database file.
	/// Can be overridden with JIRO_DataPaths__Database environment variable.
	/// </summary>
	public string Database { get; set; } = "Data/Database/jiro.db";

	/// <summary>
	/// Resolves a path to an absolute path based on the application base directory.
	/// </summary>
	/// <param name="path">The path to resolve.</param>
	/// <returns>An absolute path.</returns>
	public static string ResolvePath(string path)
	{
		return Path.IsPathRooted(path)
			? path
			: Path.Combine(AppContext.BaseDirectory, path);
	}

	/// <summary>
	/// Gets the absolute path for logs.
	/// </summary>
	public string AbsoluteLogsPath => ResolvePath(Logs);

	/// <summary>
	/// Gets the absolute path for themes.
	/// </summary>
	public string AbsoluteThemesPath => ResolvePath(Themes);

	/// <summary>
	/// Gets the absolute path for plugins.
	/// </summary>
	public string AbsolutePluginsPath => ResolvePath(Plugins);

	/// <summary>
	/// Gets the absolute path for the database.
	/// </summary>
	public string AbsoluteDatabasePath => ResolvePath(Database);
}
