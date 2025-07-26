namespace Jiro.Core.Services.Context;

/// <summary>
/// Provides contextual information about the current instance and session.
/// </summary>
public interface IInstanceContext
{
	/// <summary>
	/// Gets or sets the unique identifier of the current instance.
	/// </summary>
	string InstanceId { get; set; }

	/// <summary>
	/// Gets or sets the unique identifier of the current session (optional).
	/// </summary>
	string? SessionId { get; set; }

	/// <summary>
	/// Sets the context for the current operation.
	/// </summary>
	/// <param name="instanceId">The instance identifier.</param>
	/// <param name="sessionId">The session identifier (optional).</param>
	void SetContext(string instanceId, string? sessionId = null);

	/// <summary>
	/// Clears the current context.
	/// </summary>
	void ClearContext();
}
