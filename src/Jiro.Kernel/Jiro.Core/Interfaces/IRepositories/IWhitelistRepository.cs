using Jiro.Core.Abstraction;
using Jiro.Core.Models;

namespace Jiro.Core.Interfaces.IRepositories
{
    public interface IWhitelistRepository : IRepository<string, WhiteListEntry>
    {

    }
}