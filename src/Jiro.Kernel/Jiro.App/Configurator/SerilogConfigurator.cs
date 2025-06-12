using Serilog;
using Serilog.Events;

namespace Jiro.App.Configurator;

public static class SerilogConfigurator
{
	public static LogEventLevel GetLogEventLevel (string? setting)
	{
		if (string.IsNullOrEmpty(setting))
			return LogEventLevel.Warning;

		return setting.ToLower() switch
		{
			SerilogConstants.LogLevels.Verbose => LogEventLevel.Verbose,
			SerilogConstants.LogLevels.Debug => LogEventLevel.Debug,
			SerilogConstants.LogLevels.Information => LogEventLevel.Information,
			SerilogConstants.LogLevels.Warning => LogEventLevel.Warning,
			SerilogConstants.LogLevels.Error => LogEventLevel.Error,
			SerilogConstants.LogLevels.Fatal => LogEventLevel.Fatal,
			_ => LogEventLevel.Warning
		};
	}

	public static RollingInterval GetRollingInterval (string? setting)
	{
		if (string.IsNullOrEmpty(setting))
			return RollingInterval.Day;

		return setting.ToLower() switch
		{
			SerilogConstants.TimeIntervals.Minute => RollingInterval.Minute,
			SerilogConstants.TimeIntervals.Hour => RollingInterval.Hour,
			SerilogConstants.TimeIntervals.Day => RollingInterval.Day,
			SerilogConstants.TimeIntervals.Month => RollingInterval.Month,
			SerilogConstants.TimeIntervals.Year => RollingInterval.Year,
			SerilogConstants.TimeIntervals.Infinite => RollingInterval.Infinite,
			_ => RollingInterval.Hour
		};
	}
}

public class SerilogConstants
{
	public partial class LogLevels
	{
		public readonly static string[] Levels = { Verbose, Debug, Warning, Information, Error, Fatal };
		public const string Verbose = "verbose";
		public const string Debug = "debug";
		public const string Warning = "warning";
		public const string Information = "information";
		public const string Error = "error";
		public const string Fatal = "fatal";
	}

	public partial class TimeIntervals
	{
		public readonly static string[] Intervals = { Minute, Hour, Day, Month, Year, Infinite };
		public const string Minute = "minute";
		public const string Hour = "hour";
		public const string Day = "day";
		public const string Month = "month";
		public const string Year = "year";
		public const string Infinite = "infinite";
	}
}
