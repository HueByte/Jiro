using Jiro.Core.IRepositories;
using Jiro.Core.Models;
using Jiro.Core.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Jiro.Core.Services.Chat;

public class ChatStorageService : IChatStorageService
{
    private readonly IChatSessionRepository _chatSessionRepository;
    private readonly IMemoryCache _memoryCache;
    private readonly ChatOptions _chatOptions;

    public ChatStorageService(IChatSessionRepository chatSessionRepository, IMemoryCache memoryCache, IOptions<ChatOptions> chatOptions)
    {
        _chatSessionRepository = chatSessionRepository;
        _memoryCache = memoryCache;
        _chatOptions = chatOptions.Value;
    }


    public Task AppendMessageToSessionAsync(string sessionId, string role, string content)
    {
        Message message = new()
        {
            Role = role,
            Content = content,
            ChatSessionId = sessionId
        };

        return AppendMessagesToSessionAsync(sessionId, [message]);
    }

    public async Task AppendMessagesToSessionAsync(string sessionId, List<Message> messages)
    {
        ChatSession? session = TryGetSessionFromCache(sessionId);
        if (session is null)
        {
            session = await _chatSessionRepository.GetAsync(sessionId);
            if (session is null)
            {
                throw new JiroException("Session not found");
            }

            _memoryCache.Set(sessionId, session, new MemoryCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });
        }

        session.Messages.AddRange(messages);

        await _chatSessionRepository.UpdateAsync(session);
        await _chatSessionRepository.SaveChangesAsync();
    }

    public async Task<ChatSession> CreateSessionAsync(string ownerId)
    {
        ChatSession session = new()
        {
            Id = Guid.NewGuid().ToString(),
            UserId = ownerId
        };

        await _chatSessionRepository.AddAsync(session);
        await _chatSessionRepository.SaveChangesAsync();

        return session;
    }

    public async Task DeleteSessionAsync(string sessionId)
    {
        var session = _chatSessionRepository.GetAsync(sessionId);
        if (session is null)
        {
            throw new JiroException("Session not found");
        }

        _memoryCache.Remove(sessionId);
        await _chatSessionRepository.RemoveAsync(sessionId);
        await _chatSessionRepository.SaveChangesAsync();
    }

    public async Task<ChatSession?> GetSessionAsync(string sessionId)
    {
        return TryGetSessionFromCache(sessionId) ?? await _chatSessionRepository.GetAsync(sessionId);
    }

    public ChatSession? TryGetSessionFromCache(string sessionId)
    {
        return _memoryCache.Get<ChatSession>(sessionId);
    }

    public async Task<List<ChatSession>> GetSessionsAsync(string ownerId)
    {
        var sessions = await _chatSessionRepository.AsQueryable()
            .Where(session => session.UserId == ownerId)
            .ToListAsync();

        return sessions;
    }
}