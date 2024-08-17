using Jiro.Core.Abstraction;
using Jiro.Core.IRepositories;
using Jiro.Core.Models;

namespace Jiro.Infrastructure.Repositories
{
    public class ChatSessionRepository : BaseRepository<string, ChatSession, JiroContext>, IChatSessionRepository
    {
        public ChatSessionRepository(JiroContext context) : base(context)
        {

        }
    }
}