namespace Jiro.Core.Services.System;

/// <summary>
/// Service for retrieving application version information
/// </summary>
public interface IVersionService
{
	/// <summary>
	/// Gets the current application version from assembly metadata
	/// </summary>
	/// <returns>The application version string</returns>
	string GetVersion();
}
