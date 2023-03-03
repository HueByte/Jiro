namespace Jiro.Core.Interfaces.IServices
{
    public interface IChatService
    {
        Task<string> ChatAsync(string prompt);
    }

}