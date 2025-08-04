namespace Jiro.Core.Abstraction;

/// <summary>
/// Base class for database entities that are associated with a specific user, extending the basic DbModel functionality.
/// </summary>
/// <typeparam name="TKey">The type of the primary key for the entity.</typeparam>
/// <typeparam name="TUserKey">The type of the user identifier key.</typeparam>
public class IdentityDbModel<TKey, TUserKey> : DbModel<TKey>
	where TKey : IConvertible
	where TUserKey : IConvertible
{
	/// <summary>
	/// Gets or sets the identifier of the user who owns this entity.
	/// </summary>
	public virtual TUserKey UserId { get; set; } = default!;
}
