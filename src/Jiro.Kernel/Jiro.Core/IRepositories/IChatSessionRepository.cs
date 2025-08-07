using Jiro.Core.Abstraction;
using Jiro.Core.Models;

namespace Jiro.Core.IRepositories
{
	/// <summary>
	/// Defines the contract for chat session repository operations that manage chat session data persistence.
	/// </summary>
	public interface IChatSessionRepository : IRepository<string, ChatSession>
	{

	}
}
