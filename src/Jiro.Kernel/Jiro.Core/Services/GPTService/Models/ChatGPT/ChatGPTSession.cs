namespace Jiro.Core.Services.GPTService.Models.ChatGPT;

public class ChatGPTSession
{
    public string OwnerId { get; set; } = string.Empty;
    public ChatGPTRequest Request { get; set; } = null!;
}