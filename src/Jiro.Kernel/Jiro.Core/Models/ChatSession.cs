using Jiro.Core.Abstraction;

namespace Jiro.Core.Models;

public class ChatSession : DbModel<string>
{
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public List<Message> Messages { get; set; } = [];
	public DateTime CreatedAt { get; set; }
	public DateTime LastUpdatedAt { get; set; }
}
