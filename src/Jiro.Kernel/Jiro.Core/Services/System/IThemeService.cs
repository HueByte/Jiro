using Jiro.Core.Services.System.Models;

namespace Jiro.Core.Services.System;

/// <summary>
/// Service for managing application themes
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Retrieves available custom themes
    /// </summary>
    /// <returns>Theme response containing available themes</returns>
    Task<ThemeResponse> GetCustomThemesAsync();
}
