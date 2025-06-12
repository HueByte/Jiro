using Jiro.Core.Abstraction;

namespace Jiro.Core.Models;

public class Message : DbModel<string>
{
	public override string Id { get; set; } = default!;
	public string Content { get; set; } = default!;
	public ulong InstanceId { get; set; }
	public ulong GuildId { get; set; }
	public ulong AuthorId { get; set; }
	public bool IsUser { get; set; }
	public DateTime CreatedAt { get; set; }
	public MessageType Type { get; set; }
}
