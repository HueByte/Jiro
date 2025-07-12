namespace Jiro.App.Services;

/// <summary>
/// Interface for monitoring command execution queue
/// </summary>
public interface ICommandQueueMonitor
{
	/// <summary>
	/// Gets the current number of executing commands
	/// </summary>
	int ActiveCommandCount { get; }

	/// <summary>
	/// Gets the list of currently executing command IDs
	/// </summary>
	IEnumerable<string> ActiveCommandIds { get; }

	/// <summary>
	/// Gets the total number of commands processed since startup
	/// </summary>
	long TotalCommandsProcessed { get; }

	/// <summary>
	/// Gets the number of commands that completed successfully
	/// </summary>
	long SuccessfulCommands { get; }

	/// <summary>
	/// Gets the number of commands that failed
	/// </summary>
	long FailedCommands { get; }
}
