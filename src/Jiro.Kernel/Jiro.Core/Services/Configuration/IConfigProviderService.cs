using Jiro.Core.Services.Configuration.Models;

namespace Jiro.Core.Services.Configuration;

/// <summary>
/// Service for retrieving and updating system configuration
/// </summary>
public interface IConfigProviderService
{
    /// <summary>
    /// Retrieves current system configuration
    /// </summary>
    /// <returns>The current system configuration</returns>
    Task<SystemConfigResponse> GetConfigAsync();

    /// <summary>
    /// Updates system configuration (limited scope for security)
    /// </summary>
    /// <param name="configJson">The configuration JSON to update</param>
    /// <returns>The update response</returns>
    Task<ConfigUpdateResponse> UpdateConfigAsync(string configJson);
}
