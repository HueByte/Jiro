using System.Text.Json;

using Jiro.Core.Options;
using Jiro.Core.Services.System;
using Jiro.Core.Services.System.Models;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

namespace Jiro.Tests.ServiceTests;

public class ThemeServiceTests : IDisposable
{
	private readonly Mock<ILogger<ThemeService>> _mockLogger;
	private readonly Mock<IHostEnvironment> _mockHostEnvironment;
	private readonly Mock<IOptions<DataPathsOptions>> _mockDataPathsOptions;
	private readonly ThemeService _themeService;
	private readonly string _testThemesDirectory;
	private readonly DataPathsOptions _dataPathsOptions;

	public ThemeServiceTests()
	{
		_mockLogger = new Mock<ILogger<ThemeService>>();
		_mockHostEnvironment = new Mock<IHostEnvironment>();

		// Create a temporary directory for test themes
		_testThemesDirectory = Path.Combine(Path.GetTempPath(), "JiroTestThemes", Guid.NewGuid().ToString());
		Directory.CreateDirectory(_testThemesDirectory);

		// Setup DataPathsOptions
		_dataPathsOptions = new DataPathsOptions
		{
			Themes = _testThemesDirectory
		};
		_mockDataPathsOptions = new Mock<IOptions<DataPathsOptions>>();
		_mockDataPathsOptions.Setup(x => x.Value).Returns(_dataPathsOptions);

		// Setup host environment
		_mockHostEnvironment.Setup(x => x.ContentRootPath).Returns(_testThemesDirectory);

		_themeService = new ThemeService(
			_mockLogger.Object,
			_mockHostEnvironment.Object,
			_mockDataPathsOptions.Object);
	}

	[Fact]
	public async Task GetCustomThemesAsync_WithNoThemesDirectory_ShouldReturnEmptyList()
	{
		// Arrange
		var nonExistentDirectory = Path.Combine(Path.GetTempPath(), "NonExistentThemes");
		_dataPathsOptions.Themes = nonExistentDirectory;

		// Act
		var result = await _themeService.GetCustomThemesAsync();

		// Assert
		Assert.NotNull(result);
		Assert.NotNull(result.Themes);
		Assert.Empty(result.Themes);
	}

	[Fact]
	public async Task GetCustomThemesAsync_WithValidThemeFiles_ShouldReturnThemes()
	{
		// Arrange
		await CreateTestThemeFile("dark-theme.json", "Dark Theme", "A dark theme for Jiro", CreateDarkColorScheme());
		await CreateTestThemeFile("light-theme.json", "Light Theme", "A light theme for Jiro", CreateLightColorScheme());

		// Act
		var result = await _themeService.GetCustomThemesAsync();

		// Assert
		Assert.NotNull(result);
		Assert.NotNull(result.Themes);
		Assert.Equal(2, result.Themes.Count);

		var darkTheme = result.Themes.FirstOrDefault(t => t.Name == "Dark Theme");
		Assert.NotNull(darkTheme);
		Assert.Equal("A dark theme for Jiro", darkTheme.Description);
		Assert.NotNull(darkTheme.JsonColorScheme);

		var lightTheme = result.Themes.FirstOrDefault(t => t.Name == "Light Theme");
		Assert.NotNull(lightTheme);
		Assert.Equal("A light theme for Jiro", lightTheme.Description);
		Assert.NotNull(lightTheme.JsonColorScheme);
	}

	[Fact]
	public async Task GetCustomThemesAsync_WithTemplateFile_ShouldIgnoreTemplate()
	{
		// Arrange
		await CreateTestThemeFile("template.json", "Template", "Template file", CreateDarkColorScheme());
		await CreateTestThemeFile("custom-theme.json", "Custom Theme", "A custom theme", CreateLightColorScheme());

		// Act
		var result = await _themeService.GetCustomThemesAsync();

		// Assert
		Assert.NotNull(result);
		Assert.NotNull(result.Themes);
		Assert.Single(result.Themes);
		Assert.Equal("Custom Theme", result.Themes.First().Name);
	}

	[Fact]
	public async Task GetCustomThemesAsync_WithInvalidThemeFile_ShouldSkipInvalidFile()
	{
		// Arrange
		await CreateTestThemeFile("valid-theme.json", "Valid Theme", "A valid theme", CreateDarkColorScheme());
		
		// Create invalid theme file
		var invalidThemePath = Path.Combine(_testThemesDirectory, "invalid-theme.json");
		await File.WriteAllTextAsync(invalidThemePath, "{ invalid json content }");

		// Act
		var result = await _themeService.GetCustomThemesAsync();

		// Assert
		Assert.NotNull(result);
		Assert.NotNull(result.Themes);
		Assert.Single(result.Themes);
		Assert.Equal("Valid Theme", result.Themes.First().Name);
	}

	[Fact]
	public async Task GetCustomThemesAsync_WithMissingNameOrDescription_ShouldUseDefaults()
	{
		// Arrange
		var themeWithoutName = new
		{
			description = "Theme without name",
			colorScheme = CreateDarkColorScheme()
		};
		
		var themeWithoutDescription = new
		{
			name = "Theme Without Description",
			colorScheme = CreateLightColorScheme()
		};

		var jsonOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
		
		var themeWithoutNamePath = Path.Combine(_testThemesDirectory, "no-name-theme.json");
		await File.WriteAllTextAsync(themeWithoutNamePath, JsonSerializer.Serialize(themeWithoutName, jsonOptions));
		
		var themeWithoutDescPath = Path.Combine(_testThemesDirectory, "no-desc-theme.json");
		await File.WriteAllTextAsync(themeWithoutDescPath, JsonSerializer.Serialize(themeWithoutDescription, jsonOptions));

		// Act
		var result = await _themeService.GetCustomThemesAsync();

		// Assert
		Assert.NotNull(result);
		Assert.NotNull(result.Themes);
		Assert.Equal(2, result.Themes.Count);

		var themeWithDefaultName = result.Themes.FirstOrDefault(t => t.Name == "no-name-theme");
		Assert.NotNull(themeWithDefaultName);
		Assert.Equal("Theme without name", themeWithDefaultName.Description);

		var themeWithDefaultDesc = result.Themes.FirstOrDefault(t => t.Name == "Theme Without Description");
		Assert.NotNull(themeWithDefaultDesc);
		Assert.Equal("Custom theme", themeWithDefaultDesc.Description);
	}

	[Fact]
	public async Task GetCustomThemesAsync_WithNullColorScheme_ShouldSkipTheme()
	{
		// Arrange
		var themeWithNullColorScheme = new
		{
			name = "Invalid Theme",
			description = "Theme with null color scheme",
			colorScheme = (object?)null
		};

		var jsonOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
		var themePath = Path.Combine(_testThemesDirectory, "null-scheme-theme.json");
		await File.WriteAllTextAsync(themePath, JsonSerializer.Serialize(themeWithNullColorScheme, jsonOptions));

		// Act
		var result = await _themeService.GetCustomThemesAsync();

		// Assert
		Assert.NotNull(result);
		Assert.NotNull(result.Themes);
		Assert.Empty(result.Themes);
	}

	[Fact]
	public async Task GetCustomThemesAsync_ShouldLogInformation()
	{
		// Arrange
		await CreateTestThemeFile("test-theme.json", "Test Theme", "Test description", CreateDarkColorScheme());

		// Act
		await _themeService.GetCustomThemesAsync();

		// Assert
		_mockLogger.Verify(
			x => x.Log(
				LogLevel.Information,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Getting custom themes from themes directory")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);

		_mockLogger.Verify(
			x => x.Log(
				LogLevel.Information,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Loaded") && v.ToString()!.Contains("custom themes")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	[Fact]
	public async Task GetCustomThemesAsync_WithNonJsonFiles_ShouldIgnoreNonJsonFiles()
	{
		// Arrange
		await CreateTestThemeFile("valid-theme.json", "Valid Theme", "A valid theme", CreateDarkColorScheme());
		
		// Create non-JSON files
		var txtFilePath = Path.Combine(_testThemesDirectory, "not-a-theme.txt");
		await File.WriteAllTextAsync(txtFilePath, "This is not a JSON file");
		
		var xmlFilePath = Path.Combine(_testThemesDirectory, "not-a-theme.xml");
		await File.WriteAllTextAsync(xmlFilePath, "<xml>This is not a JSON file</xml>");

		// Act
		var result = await _themeService.GetCustomThemesAsync();

		// Assert
		Assert.NotNull(result);
		Assert.NotNull(result.Themes);
		Assert.Single(result.Themes);
		Assert.Equal("Valid Theme", result.Themes.First().Name);
	}

	private async Task CreateTestThemeFile(string fileName, string name, string description, ColorScheme colorScheme)
	{
		var theme = new
		{
			name,
			description,
			colorScheme
		};

		var jsonOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
		var json = JsonSerializer.Serialize(theme, jsonOptions);
		var filePath = Path.Combine(_testThemesDirectory, fileName);
		await File.WriteAllTextAsync(filePath, json);
	}

	private static ColorScheme CreateDarkColorScheme()
	{
		return new ColorScheme
		{
			Background = "#1a1a1a",
			BackgroundSecondary = "#2d2d2d",
			TextPrimary = "#ffffff",
			TextSecondary = "#cccccc",
			Primary = "#4a9eff",
			Secondary = "#6c757d",
			Success = "#28a745",
			Error = "#dc3545",
			Warning = "#ffc107"
		};
	}

	private static ColorScheme CreateLightColorScheme()
	{
		return new ColorScheme
		{
			Background = "#ffffff",
			BackgroundSecondary = "#f8f9fa",
			TextPrimary = "#212529",
			TextSecondary = "#6c757d",
			Primary = "#007bff",
			Secondary = "#6c757d",
			Success = "#28a745",
			Error = "#dc3545",
			Warning = "#ffc107"
		};
	}

	public void Dispose()
	{
		if (Directory.Exists(_testThemesDirectory))
		{
			Directory.Delete(_testThemesDirectory, recursive: true);
		}
	}
}