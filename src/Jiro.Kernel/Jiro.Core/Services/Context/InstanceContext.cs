namespace Jiro.Core.Services.Context;

/// <summary>
/// Provides contextual information about the current instance and session.
/// This is a scoped service that maintains context throughout a request/operation.
/// </summary>
public class InstanceContext : IInstanceContext
{
	/// <summary>
	/// Gets or sets the unique identifier of the current instance.
	/// </summary>
	public string InstanceId { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the unique identifier of the current session (optional).
	/// </summary>
	public string? SessionId { get; set; }

	/// <summary>
	/// Sets the context for the current operation.
	/// </summary>
	/// <param name="instanceId">The instance identifier.</param>
	/// <param name="sessionId">The session identifier (optional).</param>
	public void SetContext(string instanceId, string? sessionId = null)
	{
		InstanceId = instanceId ?? throw new ArgumentNullException(nameof(instanceId));
		SessionId = sessionId;
	}

	/// <summary>
	/// Clears the current context.
	/// </summary>
	public void ClearContext()
	{
		InstanceId = string.Empty;
		SessionId = null;
	}
}
