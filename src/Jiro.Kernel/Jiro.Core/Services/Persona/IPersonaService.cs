namespace Jiro.Core.Services.Persona;

/// <summary>
/// Defines the contract for persona management services that handle AI personality configuration and updates.
/// </summary>
public interface IPersonaService
{
	/// <summary>
	/// Adds a summary or update message to the persona configuration.
	/// </summary>
	/// <param name="updateMessage">The message to add to the persona summary.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	Task AddSummaryAsync(string updateMessage);

	/// <summary>
	/// Retrieves the persona configuration for the specified instance.
	/// </summary>
	/// <param name="instanceId">The unique identifier of the instance. Uses default if empty.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the persona configuration as a string.</returns>
	Task<string> GetPersonaAsync(string instanceId = "");
}
