using OpenAI.Chat;

namespace Jiro.Core.Services.GPTService;

[Obsolete]
public class DisabledGptService : IChatService
{
    public Task<string> ChatAsync(string prompt)
    {
        return Task.FromResult("The chat functionality is currently disabled.");
    }

    public Task<string> ChatAsync(string prompt, string sessionId)
    {
        throw new NotImplementedException();
    }

    Task<Message> IChatService.ChatAsync(string prompt, string sessionId)
    {
        throw new NotImplementedException();
    }
}