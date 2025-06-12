using Jiro.Core.Abstraction;

namespace Jiro.Core.Models;

public class ChatSession : DbModel<string>
{
	public string UserId { get; set; } = default!;
	public List<Message> Messages { get; set; } = [];
}
