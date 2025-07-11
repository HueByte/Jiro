using Jiro.Core.Abstraction;
using Jiro.Core.IRepositories;
using Jiro.Core.Models;

namespace Jiro.Infrastructure.Repositories
{
	/// <summary>
	/// Repository implementation for managing message entities in the database.
	/// Provides data access operations for messages extending the base repository functionality.
	/// </summary>
	public class MessageRepository : BaseRepository<string, Message, JiroContext>, IMessageRepository
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MessageRepository"/> class.
		/// </summary>
		/// <param name="context">The database context for message operations.</param>
		public MessageRepository(JiroContext context) : base(context)
		{

		}
	}
}
