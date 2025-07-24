namespace Jiro.Core.Options;

/// <summary>
/// Configuration options for core application settings including API configuration.
/// These values can be overridden using JIRO_ prefixed environment variables.
/// </summary>
public class ApplicationOptions : IOption
{
	/// <summary>
	/// Configuration section name for application settings.
	/// This class maps to root-level configuration properties.
	/// </summary>
	public const string Application = "";

	/// <summary>
	/// Gets or sets the API key for Jiro service authentication.
	/// Can be overridden with JIRO_ApiKey environment variable.
	/// </summary>
	public string ApiKey { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the Jiro API base URL.
	/// Can be overridden with JIRO_JiroApi environment variable.
	/// </summary>
	public string JiroApi { get; set; } = "https://localhost:18092";

	/// <summary>
	/// Gets or sets the tokenizer service URL.
	/// Can be overridden with JIRO_TokenizerUrl environment variable.
	/// </summary>
	public string TokenizerUrl { get; set; } = "http://localhost:8000";

	/// <summary>
	/// Gets or sets whether whitelist functionality is enabled.
	/// Can be overridden with JIRO_Whitelist environment variable.
	/// </summary>
	public bool Whitelist { get; set; } = true;

	/// <summary>
	/// Validates that required configuration values are provided.
	/// </summary>
	/// <returns>True if configuration is valid, false otherwise.</returns>
	public bool IsValid()
	{
		return !string.IsNullOrWhiteSpace(ApiKey) && 
		       !string.IsNullOrWhiteSpace(JiroApi);
	}

	/// <summary>
	/// Gets validation error messages for missing required configuration.
	/// </summary>
	/// <returns>List of validation error messages.</returns>
	public IEnumerable<string> GetValidationErrors()
	{
		var errors = new List<string>();

		if (string.IsNullOrWhiteSpace(ApiKey))
			errors.Add("ApiKey is required. Set it in appsettings.json or use JIRO_ApiKey environment variable.");

		if (string.IsNullOrWhiteSpace(JiroApi))
			errors.Add("JiroApi is required. Set it in appsettings.json or use JIRO_JiroApi environment variable.");

		return errors;
	}
}