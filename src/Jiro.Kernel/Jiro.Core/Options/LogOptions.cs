namespace Jiro.Core.Options;

public class LogOptions : IOption
{
	public const string Log = "Log";

	public string LogLevel { get; set; } = string.Empty;
	public string TimeInterval { get; set; } = string.Empty;
	public string AspNetCoreLevel { get; set; } = string.Empty;
	public string DatabaseLevel { get; set; } = string.Empty;
	public string SystemLevel { get; set; } = string.Empty;
}
