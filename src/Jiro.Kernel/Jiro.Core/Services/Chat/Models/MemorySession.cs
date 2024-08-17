namespace Jiro.Core.Services.Chat.Models;

public class MemorySession
{
    public string OwnerId { get; set; } = default!;
    public string SessionId { get; set; } = default!;
    public List<OpenAI.Chat.Message> Messages { get; set; } = new();
}