namespace Jiro.Core.Services.CommandHandler;

/// <summary>
/// Defines the contract for command handling services that process and execute user commands.
/// </summary>
public interface ICommandHandlerService
{
	/// <summary>
	/// Occurs when a command execution event needs to be logged.
	/// </summary>
	event Action<string, object[]>? OnLog;

	/// <summary>
	/// Executes a command based on the provided prompt using the specified service provider scope.
	/// </summary>
	/// <param name="scopedProvider">The scoped service provider containing the required dependencies.</param>
	/// <param name="prompt">The user prompt or command to execute.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the command response.</returns>
	Task<CommandResponse> ExecuteCommandAsync(IServiceProvider scopedProvider, string prompt);
}
