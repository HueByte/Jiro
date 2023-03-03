using System.Collections.Concurrent;
using Jiro.Core.Interfaces.IServices;
using Jiro.Core.Options;
using Jiro.Core.Services.GPTService.Models.ChatGPT;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jiro.Core.Services.GPTService
{
    public class ChatGPTStorageService : IChatGPTStorageService
    {
        private readonly ILogger _logger;
        private readonly ChatGptOptions _chatGptOptions;
        private readonly ConcurrentDictionary<string, ChatGPTSession> _sessions = new();
        public ChatGPTStorageService(ILogger<ChatGPTStorageService> logger, IOptions<ChatGptOptions> chatGptOptions)
        {
            _logger = logger;
            _chatGptOptions = chatGptOptions.Value;
        }

        public ChatGPTSession GetOrCreateSession(string userId)
        {
            if (_sessions.ContainsKey(userId))
                return _sessions[userId];

            ChatMessage systemMessage = new()
            {
                Role = "system",
                Content = "You are AI chatting bot called Jiro"
            };

            ChatGPTRequest req = new()
            {
                Model = "gpt-3.5-turbo",
                Messages = new() { systemMessage },
                MaxTokens = _chatGptOptions.TokenLimit
            };

            ChatGPTSession session = new()
            {
                OwnerId = userId,
                Request = req
            };

            AddSession(userId, session);

            return session;
        }

        public void AddSession(string userId, ChatGPTSession session)
        {
            _sessions.TryAdd(userId, session);
        }

        public void RemoveSession(string userId)
        {
            _sessions.TryRemove(userId, out _);
        }

        public void UpdateSession(string userId, ChatGPTSession session)
        {
            _sessions[userId] = session;
        }

        public void GetSession(string userId, out ChatGPTSession session)
        {
            _sessions.TryGetValue(userId, out session);
        }
    }
}