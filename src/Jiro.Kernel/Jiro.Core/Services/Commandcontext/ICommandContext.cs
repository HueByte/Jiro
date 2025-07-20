namespace Jiro.Core.Services.CommandContext;

/// <summary>
/// Defines the contract for command context services that maintain execution state and metadata during command processing.
/// </summary>
public interface ICommandContext
{
	/// <summary>
	/// Gets the unique identifier of the current instance.
	/// </summary>
	string InstanceId { get; }

	/// <summary>
	/// Gets the unique identifier of the current session.
	/// </summary>
	string SessionId { get; }

	/// <summary>
	/// Gets the collection of contextual data associated with the current command execution.
	/// </summary>
	Dictionary<string, object> Data { get; }

	/// <summary>
	/// Sets the current instance identifier for command execution context.
	/// </summary>
	/// <param name="instanceId">The instance identifier to set, or null to clear.</param>
	void SetCurrentInstance(string? instanceId);

	/// <summary>
	/// Sets the contextual data for the current command execution.
	/// </summary>
	/// <param name="data">The collection of key-value pairs to set as contextual data.</param>
	void SetData(IEnumerable<KeyValuePair<string, object>> data);

	/// <summary>
	/// Sets the session identifier for the current command execution context.
	/// </summary>
	/// <param name="sessionId">The session identifier to set.</param>
	void SetSessionId(string sessionId);
}
