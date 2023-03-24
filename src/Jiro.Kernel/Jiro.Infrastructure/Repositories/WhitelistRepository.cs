using Jiro.Core.Abstraction;
using Jiro.Core.Interfaces.IRepositories;
using Jiro.Core.Models;

namespace Jiro.Infrastructure.Repositories
{
    public class WhitelistRepository : BaseRepository<string, WhiteListEntry, JiroContext>, IWhitelistRepository
    {
        public WhitelistRepository(JiroContext context) : base(context) { }
    }
}