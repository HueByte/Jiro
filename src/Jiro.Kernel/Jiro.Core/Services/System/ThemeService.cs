using System.Text.Json;

using Jiro.Core.Services.System.Models;
using Jiro.Shared.Websocket.Requests;
using Jiro.Shared.Websocket.Responses;

using Microsoft.Extensions.Logging;

namespace Jiro.Core.Services.System;

/// <inheritdoc/>
public class ThemeService : IThemeService
{
	private readonly ILogger<ThemeService> _logger;

	/// <summary>
	/// Initializes a new instance of the <see cref="ThemeService"/> class.
	/// </summary>
	/// <param name="logger">Logger instance for logging.</param>
	public ThemeService(ILogger<ThemeService> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	/// <inheritdoc/>
	public Task<ThemesResponse> GetCustomThemesAsync()
	{
		try
		{
			_logger.LogInformation("Getting custom themes");

			// This is a placeholder implementation
			// In a real system, you'd read theme files from a themes directory
			var themes = new List<Theme>
			{
				new Theme
				{
					Name = "Dark Mode",
					Description = "A dark theme for low light environments",
					JsonColorScheme = ColorSchemeToJson(new ColorScheme
					{
						Transparent = "#00000000",
						Primary = "#BB86FC",
						Secondary = "#03DAC6",
						Error = "#CF6679",
						Background = "#121212",
						Surface = "#1E1E1E"
					})
				}
			};

			var response = new ThemesResponse
			{
				Themes = themes
			};

			return Task.FromResult(response);
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
