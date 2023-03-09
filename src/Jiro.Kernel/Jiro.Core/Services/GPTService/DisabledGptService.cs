namespace Jiro.Core.Services.GPTService
{
    public class DisabledGptService : IChatService
    {
        public Task<string> ChatAsync(string prompt)
        {
            return Task.FromResult("The chat functionality is currently disabled.");
        }
    }
}