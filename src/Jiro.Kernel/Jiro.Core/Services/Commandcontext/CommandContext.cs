namespace Jiro.Core.Services.CommandContext;

public class CommandContext : ICommandContext
{
	public string InstanceId { get; private set; } = string.Empty;
	public string SessionId { get; private set; } = string.Empty;
	public Dictionary<string, object> Data { get; } = [];

	public void SetData (IEnumerable<KeyValuePair<string, object>> data)
	{
		foreach ((string key, object value) in data)
		{
			if (!Data.TryAdd(key, value))
				Data[key] = value;
		}
	}

	public void SetCurrentInstance (string? instanceId)
	{
		if (string.IsNullOrEmpty(instanceId))
			throw new JiroException(new ArgumentException(null, nameof(instanceId)), "Something went wrong with parsing current instance", "Try to relogin");

		InstanceId = instanceId;
	}

	public void SetSessionId (string sessionId)
	{
		if (string.IsNullOrEmpty(sessionId))
			throw new JiroException(new ArgumentException(null, nameof(sessionId)), "Something went wrong with parsing session", "Try to relogin");

		SessionId = sessionId;
	}
}
