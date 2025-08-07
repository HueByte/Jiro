
using Jiro.Shared.Websocket.Responses;

namespace Jiro.Core.Services.System;

/// <summary>
/// Service for managing system configuration
/// </summary>
public interface IConfigProviderService
{
	/// <summary>
	/// Retrieves current system configuration
	/// </summary>
	/// <returns>System configuration response</returns>
	Task<ConfigResponse> GetConfigAsync();

	/// <summary>
	/// Updates system configuration (read-only implementation for security)
	/// </summary>
	/// <param name="configJson">Configuration JSON string</param>
	/// <returns>Configuration update response</returns>
	Task<ConfigUpdateResponse> UpdateConfigAsync(string configJson);
}
