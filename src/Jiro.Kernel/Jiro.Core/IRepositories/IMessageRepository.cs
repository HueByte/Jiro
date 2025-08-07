using Jiro.Core.Abstraction;
using Jiro.Core.Models;

namespace Jiro.Core.IRepositories
{
	/// <summary>
	/// Defines the contract for message repository operations that manage chat message data persistence.
	/// </summary>
	public interface IMessageRepository : IRepository<string, Message>
	{

	}
}
