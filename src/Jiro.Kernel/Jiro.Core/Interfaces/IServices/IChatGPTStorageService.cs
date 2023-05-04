using Jiro.Core.Services.GPTService.Models.ChatGPT;

namespace Jiro.Core.Interfaces.IServices
{
    public interface IChatGPTStorageService
    {
        ChatGPTSession? GetOrCreateSession(string userId);
        void AddSession(string userId, ChatGPTSession session);
        void GetSession(string userId, out ChatGPTSession? session);
        void RemoveSession(string userId);
        void UpdateSession(string userId, ChatGPTSession session);
    }
}