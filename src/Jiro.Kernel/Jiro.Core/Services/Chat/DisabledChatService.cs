
namespace Jiro.Core.Services.Chat;

public class DisabledChatService : IChatService
{
    public Task<OpenAI.Chat.Message> ChatAsync(string prompt, string sessionId)
    {
        return Task.FromResult(new OpenAI.Chat.Message(OpenAI.Role.Assistant, "The chat functionality is currently disabled", null));
    }

    public Task<string> ChatAsync(string prompt)
    {
        return Task.FromResult("The chat functionality is currently disabled");
    }
}