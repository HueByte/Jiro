namespace Jiro.Core.Abstraction;

/// <summary>
/// Defines the contract for an identity-aware repository that extends the basic repository functionality 
/// with user context filtering capabilities.
/// </summary>
/// <typeparam name="Tkey">The type of the entity's key that must implement <see cref="IConvertible"/>.</typeparam>
/// <typeparam name="TEntity">The type of the entity that must inherit from IdentityDbModel.</typeparam>
public interface IIdentityRepository<Tkey, TEntity> : IRepository<Tkey, TEntity>
	where Tkey : IConvertible
	where TEntity : IdentityDbModel<Tkey, string>
{
	/// <summary>
	/// Returns an <see cref="IQueryable{T}"/> for the entity type filtered by the current user's identity.
	/// </summary>
	/// <returns>An <see cref="IQueryable{T}"/> instance containing only entities belonging to the current user.</returns>
	IQueryable<TEntity> AsIdentityQueryable();
}
