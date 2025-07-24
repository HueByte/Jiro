using System.Text.Json;

using Jiro.Core.Options;
using Jiro.Core.Services.System.Models;
using Jiro.Shared.Websocket.Requests;
using Jiro.Shared.Websocket.Responses;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jiro.Core.Services.System;

/// <inheritdoc/>
public class ThemeService : IThemeService
{
	private readonly ILogger<ThemeService> _logger;
	private readonly IHostEnvironment _hostEnvironment;
	private readonly DataPathsOptions _dataPathsOptions;

	/// <summary>
	/// Initializes a new instance of the <see cref="ThemeService"/> class.
	/// </summary>
	/// <param name="logger">Logger instance for logging.</param>
	/// <param name="hostEnvironment">Host environment to get content root path.</param>
	/// <param name="dataPathsOptions">Data paths configuration options.</param>
	public ThemeService(ILogger<ThemeService> logger, IHostEnvironment hostEnvironment, IOptions<DataPathsOptions> dataPathsOptions)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
		_dataPathsOptions = dataPathsOptions?.Value ?? throw new ArgumentNullException(nameof(dataPathsOptions));
	}

	/// <inheritdoc/>
	public async Task<ThemesResponse> GetCustomThemesAsync()
	{
		try
		{
			_logger.LogInformation("Getting custom themes from themes directory");

			var themes = new List<Theme>();
			// Use the configured themes path from options (JIRO_ env vars automatically override)
			var themesPath = _dataPathsOptions.AbsoluteThemesPath;

			if (!Directory.Exists(themesPath))
			{
				_logger.LogWarning("Themes directory not found at: {ThemesPath}", themesPath);
				return new ThemesResponse { Themes = themes };
			}

			var jsonFiles = Directory.GetFiles(themesPath, "*.json")
				.Where(file => !Path.GetFileName(file).Equals("template.json", StringComparison.OrdinalIgnoreCase));

			foreach (var filePath in jsonFiles)
			{
				try
				{
					var jsonContent = await File.ReadAllTextAsync(filePath);
					var themeData = JsonSerializer.Deserialize<ThemeFile>(jsonContent, new JsonSerializerOptions
					{
						PropertyNameCaseInsensitive = true
					});

					if (themeData?.ColorScheme != null)
					{
						var theme = new Theme
						{
							Name = themeData.Name ?? Path.GetFileNameWithoutExtension(filePath),
							Description = themeData.Description ?? "Custom theme",
							JsonColorScheme = JsonSerializer.Serialize(themeData.ColorScheme, new JsonSerializerOptions
							{
								WriteIndented = true,
								PropertyNamingPolicy = JsonNamingPolicy.CamelCase
							})
						};

						themes.Add(theme);
						_logger.LogDebug("Loaded theme: {ThemeName} from {FilePath}", theme.Name, filePath);
					}
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Failed to load theme from file: {FilePath}", filePath);
				}
			}

			_logger.LogInformation("Loaded {ThemeCount} custom themes", themes.Count);

			return new ThemesResponse { Themes = themes };
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving custom themes");
			throw;
		}
	}

	private string ColorSchemeToJson(ColorScheme colorScheme)
	{
		// Convert the color scheme to JSON format
		return JsonSerializer.Serialize(colorScheme, new JsonSerializerOptions
		{
			WriteIndented = true
		});
	}
}

/// <summary>
/// Represents the structure of a theme JSON file.
/// </summary>
internal class ThemeFile
{
	/// <summary>
	/// Gets or sets the theme name.
	/// </summary>
	public string? Name { get; set; }

	/// <summary>
	/// Gets or sets the theme description.
	/// </summary>
	public string? Description { get; set; }

	/// <summary>
	/// Gets or sets the color scheme for the theme.
	/// </summary>
	public ColorScheme? ColorScheme { get; set; }
}
