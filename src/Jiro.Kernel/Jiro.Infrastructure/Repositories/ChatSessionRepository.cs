using Jiro.Core.Abstraction;
using Jiro.Core.IRepositories;
using Jiro.Core.Models;

namespace Jiro.Infrastructure.Repositories
{
	/// <summary>
	/// Repository implementation for managing chat session entities in the database.
	/// Provides data access operations for chat sessions extending the base repository functionality.
	/// </summary>
	public class ChatSessionRepository : BaseRepository<string, ChatSession, JiroContext>, IChatSessionRepository
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChatSessionRepository"/> class.
		/// </summary>
		/// <param name="context">The database context for chat session operations.</param>
		public ChatSessionRepository(JiroContext context) : base(context)
		{

		}
	}
}
