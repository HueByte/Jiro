namespace Jiro.Core.Abstraction;

/// <summary>
/// Defines the contract for a generic repository that provides basic CRUD operations for entities.
/// </summary>
/// <typeparam name="Tkey">The type of the entity's key that must implement <see cref="IConvertible"/>.</typeparam>
/// <typeparam name="TEntity">The type of the entity that must inherit from <see cref="DbModel{Tkey}"/>.</typeparam>
public interface IRepository<Tkey, TEntity>
	where Tkey : IConvertible
	where TEntity : DbModel<Tkey>
{
	/// <summary>
	/// Returns an <see cref="IQueryable{T}"/> for the entity type to enable complex queries.
	/// </summary>
	/// <returns>An <see cref="IQueryable{T}"/> instance for the entity type.</returns>
	IQueryable<TEntity> AsQueryable();

	/// <summary>
	/// Retrieves an entity by its unique identifier.
	/// </summary>
	/// <param name="id">The unique identifier of the entity.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the entity or null if not found.</returns>
	Task<TEntity?> GetAsync(Tkey id);

	/// <summary>
	/// Adds a new entity to the repository.
	/// </summary>
	/// <param name="entity">The entity to add.</param>
	/// <returns>A task that represents the asynchronous operation. The task result indicates whether the operation was successful.</returns>
	Task<bool> AddAsync(TEntity? entity);

	/// <summary>
	/// Adds multiple entities to the repository.
	/// </summary>
	/// <param name="entities">The collection of entities to add.</param>
	/// <returns>A task that represents the asynchronous operation. The task result indicates whether the operation was successful.</returns>
	Task<bool> AddRangeAsync(IEnumerable<TEntity> entities);

	/// <summary>
	/// Removes an entity from the repository by its unique identifier.
	/// </summary>
	/// <param name="id">The unique identifier of the entity to remove.</param>
	/// <returns>A task that represents the asynchronous operation. The task result indicates whether the operation was successful.</returns>
	Task<bool> RemoveAsync(Tkey id);

	/// <summary>
	/// Removes the specified entity from the repository.
	/// </summary>
	/// <param name="entity">The entity to remove.</param>
	/// <returns>A task that represents the asynchronous operation. The task result indicates whether the operation was successful.</returns>
	Task<bool> RemoveAsync(TEntity? entity);

	/// <summary>
	/// Removes multiple entities from the repository.
	/// </summary>
	/// <param name="entity">The collection of entities to remove.</param>
	/// <returns>A task that represents the asynchronous operation. The task result indicates whether the operation was successful.</returns>
	Task<bool> RemoveRangeAsync(IEnumerable<TEntity> entity);

	/// <summary>
	/// Updates an existing entity in the repository.
	/// </summary>
	/// <param name="entity">The entity to update.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	Task UpdateAsync(TEntity? entity);

	/// <summary>
	/// Updates multiple entities in the repository.
	/// </summary>
	/// <param name="entities">The collection of entities to update.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	Task UpdateRange(IEnumerable<TEntity> entities);

	/// <summary>
	/// Saves all pending changes to the underlying data store.
	/// </summary>
	/// <returns>A task that represents the asynchronous operation.</returns>
	Task SaveChangesAsync();
}
