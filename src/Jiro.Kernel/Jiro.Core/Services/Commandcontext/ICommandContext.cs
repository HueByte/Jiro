namespace Jiro.Core.Services.CommandContext;

public interface ICommandContext
{
	string InstanceId { get; }
	string SessionId { get; }
	Dictionary<string, object> Data { get; }

	void SetCurrentInstance(string? instanceId);
	void SetData(IEnumerable<KeyValuePair<string, object>> data);
	void SetSessionId(string sessionId);
}
