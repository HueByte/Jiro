using Jiro.Core.Abstraction;

namespace Jiro.Core.Models;

public class Message : DbModel<string>
{
	public override string Id { get; set; } = default!;
	public string Content { get; set; } = default!;
	public string InstanceId { get; set; } = default!;
	public string SessionId { get; set; } = default!;
	public bool IsUser { get; set; }
	public DateTime CreatedAt { get; set; }
	public MessageType Type { get; set; }
}
