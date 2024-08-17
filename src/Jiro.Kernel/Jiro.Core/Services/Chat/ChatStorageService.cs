using Jiro.Core.IRepositories;
using Jiro.Core.Models;
using Jiro.Core.Options;
using Jiro.Core.Services.Chat.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OpenAI;

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

    public async Task AppendMessageAsync(string sessionId, string role, string content)
    {
        await AppendMessagesAsync(sessionId, [new OpenAI.Chat.Message(GetMessageRole(role), content)]);
    }

    public async Task AppendMessagesAsync(string sessionId, IEnumerable<OpenAI.Chat.Message> messages)
    {
        var persistentSession = await _chatSessionRepository.AsQueryable()
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (persistentSession is null)
        {
            throw new JiroException("Session not found");
        }

        var memorySession = await GetSessionAsync(sessionId);

        persistentSession.Messages.AddRange(messages.Select(m => new Message
        {
            Role = m.Role.ToString(),
            Content = m.Content
        }));

        await _chatSessionRepository.UpdateAsync(persistentSession);
        await _chatSessionRepository.SaveChangesAsync();

        if (memorySession is not null)
        {
            memorySession.Messages.AddRange(messages);
            _memoryCache.Set(sessionId, memorySession, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
            });
        }
    }

    public async Task<ChatSession> CreateSessionAsync(string ownerId)
    {
        ChatSession session = new()
        {
            SessionId = Guid.NewGuid().ToString(),
            UserId = ownerId,
            Messages = new List<Message>()
        };

        session.Messages.Add(new Message
        {
            Role = "System",
            Content = _chatOptions.SystemMessage
        });

        await _chatSessionRepository.AddAsync(session);
        await _chatSessionRepository.SaveChangesAsync();

        return session;
    }

    public async Task<MemorySession?> GetSessionAsync(string sessionId, bool withMessages = false)
    {
        var session = _memoryCache.Get<MemorySession>(sessionId);
        if (session is null)
        {
            var persistentSession = await _chatSessionRepository.AsQueryable()
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (persistentSession is null)
                return null;

            session = new MemorySession()
            {
                OwnerId = persistentSession.UserId,
                SessionId = persistentSession.SessionId,
            };

            foreach (var message in persistentSession.Messages)
            {
                session.Messages.Add(new OpenAI.Chat.Message(GetMessageRole(message.Role), message.Content));
            }

            _memoryCache.Set(sessionId, session, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
            });
        }

        return session;
    }

    public Task<List<string>> GetSessionIdsAsync(string ownerId)
    {
        return _chatSessionRepository.AsQueryable()
            .Where(s => s.UserId == ownerId)
            .Select(s => s.SessionId)
            .ToListAsync();
    }

    public async Task RemoveSessionAsync(string sessionId)
    {
        await _chatSessionRepository.RemoveAsync(sessionId);
        await _chatSessionRepository.SaveChangesAsync();
    }

    public async Task UpdateSessionAsync(ChatSession session)
    {
        await _chatSessionRepository.UpdateAsync(session);
        await _chatSessionRepository.SaveChangesAsync();
    }

    private Role GetMessageRole(string value) => value switch
    {
        "User" => Role.User,
        "System" => Role.System,
        "AI" => Role.Assistant,
        _ => throw new ArgumentException($"Invalid value {value}", nameof(value))
    };
}