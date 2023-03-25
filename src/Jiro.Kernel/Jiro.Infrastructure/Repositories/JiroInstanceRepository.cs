using Jiro.Core.Abstraction;
using Jiro.Core.Interfaces.IRepositories;
using Jiro.Core.Models;

namespace Jiro.Infrastructure.Repositories
{
    public class JiroInstanceRepository : BaseRepository<int, JiroInstance, JiroContext>, IJiroInstanceRepository
    {
        public JiroInstanceRepository(JiroContext context) : base(context) { }
    }
}