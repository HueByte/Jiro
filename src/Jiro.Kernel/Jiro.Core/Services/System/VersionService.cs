using System.Reflection;

namespace Jiro.Core.Services.System;

/// <summary>
/// Service for retrieving application version information from assembly metadata
/// </summary>
public class VersionService : IVersionService
{
	private readonly string _version;

	public VersionService()
	{
		_version = GetVersionFromAssembly();
	}

	/// <summary>
	/// Gets the current application version from assembly metadata
	/// </summary>
	/// <returns>The application version string</returns>
	public string GetVersion() => _version;

	private static string GetVersionFromAssembly()
	{
		var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
		var version = assembly.GetName().Version;
		
		// Try to get informational version first (includes pre-release info like "0.1.1-beta")
		var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
		
		return informationalVersion ?? version?.ToString() ?? "Unknown";
	}
}