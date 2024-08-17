namespace Jiro.Core.Interfaces.IServices;

public interface IChatService
{
    Task<OpenAI.Chat.Message> ChatAsync(string prompt, string sessionId);

    [Obsolete]
    Task<string> ChatAsync(string prompt);
}