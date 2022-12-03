namespace Jiro.Core.Interfaces.IServices
{
    public interface IGPTService
    {
        Task<string> ChatAsync(string prompt);
    }

}