using Jiro.Core.Abstraction;
using Jiro.Core.IRepositories;
using Jiro.Core.Models;

namespace Jiro.Infrastructure.Repositories
{
	public class MessageRepository : BaseRepository<string, Message, JiroContext>, IMessageRepository
	{
		public MessageRepository(JiroContext context) : base(context)
		{

		}
	}
}
