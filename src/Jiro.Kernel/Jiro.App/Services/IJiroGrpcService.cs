using Jiro.Commands.Models;

namespace Jiro.App.Services;

/// <summary>
/// Interface for sending command results via gRPC
/// </summary>
public interface IJiroGrpcService
{
	/// <summary>
	/// Sends a successful command result to the server
	/// </summary>
	/// <param name="commandSyncId">The command synchronization ID</param>
	/// <param name="commandResult">The command execution result</param>
	/// <returns>A task representing the async operation</returns>
	Task SendCommandResultAsync(string commandSyncId, CommandResponse commandResult);

	/// <summary>
	/// Sends an error result to the server
	/// </summary>
	/// <param name="commandSyncId">The command synchronization ID</param>
	/// <param name="errorMessage">The error message</param>
	/// <returns>A task representing the async operation</returns>
	Task SendCommandErrorAsync(string commandSyncId, string errorMessage);
}
