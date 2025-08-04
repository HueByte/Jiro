using Microsoft.EntityFrameworkCore;

namespace Jiro.Core.Abstraction;

/// <summary>
/// Base repository implementation providing common data access operations.
/// </summary>
/// <typeparam name="TKeyType">The type of the entity's primary key.</typeparam>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
/// <typeparam name="TContext">The type of the database context.</typeparam>
public class BaseRepository<TKeyType, TEntity, TContext> : IRepository<TKeyType, TEntity>
	where TKeyType : IConvertible
	where TEntity : DbModel<TKeyType>, new()
	where TContext : DbContext, new()
{
	/// <summary>
	/// The database context used for data access.
	/// </summary>
	protected internal readonly TContext _context;

	/// <summary>
	/// Initializes a new instance of the <see cref="BaseRepository{TKeyType, TEntity, TContext}"/> class.
	/// </summary>
	/// <param name="context">The database context.</param>
	public BaseRepository(TContext context)
	{
		_context = context ?? new TContext();
	}

	/// <summary>
	/// Adds a new entity to the repository.
	/// </summary>
	/// <param name="entity">The entity to add.</param>
	/// <returns>True if the entity was added successfully, false otherwise.</returns>
	public virtual async Task<bool> AddAsync(TEntity? entity)
	{
		if (entity is null)
			return false;

		var doesExist = await _context
			.Set<TEntity>()
			.AnyAsync(entry => entry.Id.Equals(entity.Id));

		if (doesExist)
			return false;

		_context
			.Set<TEntity>()
			.Add(entity);

		return true;
	}

	/// <summary>
	/// Adds a collection of entities to the repository.
	/// </summary>
	/// <param name="entities">The entities to add.</param>
	/// <returns>True if the entities were added successfully, false otherwise.</returns>
	public virtual async Task<bool> AddRangeAsync(IEnumerable<TEntity> entities)
	{
		if (entities is null)
			return false;

		await _context
			.Set<TEntity>()
			.AddRangeAsync(entities);

		return true;
	}

	/// <summary>
	/// Gets an IQueryable for the entity set.
	/// </summary>
	/// <returns>An IQueryable of entities.</returns>
	public virtual IQueryable<TEntity> AsQueryable()
	{
		return _context.Set<TEntity>().AsQueryable();
	}

	/// <summary>
	/// Gets an entity by its primary key.
	/// </summary>
	/// <param name="id">The primary key value.</param>
	/// <returns>The entity if found, null otherwise.</returns>
	public virtual Task<TEntity?> GetAsync(TKeyType id)
	{
		return _context
			.Set<TEntity>()
			.FirstOrDefaultAsync(entry => entry.Id.Equals(id));
	}

	/// <summary>
	/// Removes an entity by its primary key.
	/// </summary>
	/// <param name="id">The primary key value.</param>
	/// <returns>True if the entity was removed successfully, false otherwise.</returns>
	public virtual async Task<bool> RemoveAsync(TKeyType id)
	{
		TEntity entity = new()
		{
			Id = id
		};

		var doesExist = await _context
			.Set<TEntity>()
			.AnyAsync(entry => entry.Id.Equals(entity.Id));

		if (!doesExist)
			return false;

		_context
			.Set<TEntity>()
			.Remove(entity);

		return true;
	}

	/// <summary>
	/// Removes an entity from the repository.
	/// </summary>
	/// <param name="entity">The entity to remove.</param>
	/// <returns>True if the entity was removed successfully, false otherwise.</returns>
	public virtual async Task<bool> RemoveAsync(TEntity? entity)
	{
		if (entity is null)
			return false;

		var doesExist = await _context
			.Set<TEntity>()
			.AnyAsync(entry => entry.Id.Equals(entity.Id));

		if (!doesExist)
			return false;

		_context
			.Set<TEntity>()
			.Remove(entity);

		return true;
	}

	/// <summary>
	/// Updates an existing entity in the repository.
	/// </summary>
	/// <param name="entity">The entity to update.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public virtual Task UpdateAsync(TEntity? entity)
	{
		if (entity is null)
			return Task.CompletedTask;

		_context
			.Set<TEntity>()
			.Update(entity);

		return Task.CompletedTask;
	}

	/// <summary>
	/// Updates a collection of entities in the repository.
	/// </summary>
	/// <param name="entities">The entities to update.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public virtual Task UpdateRange(IEnumerable<TEntity> entities)
	{
		_context
			.Set<TEntity>()
			.UpdateRange(entities);

		return Task.CompletedTask;
	}

	/// <summary>
	/// Saves all pending changes to the database.
	/// </summary>
	/// <returns>A task representing the asynchronous save operation.</returns>
	public virtual Task SaveChangesAsync()
	{
		return _context.SaveChangesAsync();
	}

	/// <summary>
	/// Removes a collection of entities from the repository.
	/// </summary>
	/// <param name="entity">The entities to remove.</param>
	/// <returns>True if the entities were removed successfully, false otherwise.</returns>
	public Task<bool> RemoveRangeAsync(IEnumerable<TEntity> entity)
	{
		if (entity is null)
			return Task.FromResult(false);

		_context
			.Set<TEntity>()
			.RemoveRange(entity);

		return Task.FromResult(true);
	}
}
