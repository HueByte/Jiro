using Jiro.Core.Models;
using Jiro.Core.Services.CommandContext;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Jiro.Core.Abstraction;

/// <summary>
/// Base repository implementation for identity-aware entities that provides CRUD operations with user context filtering.
/// </summary>
/// <typeparam name="TKeyType">The type of the primary key for the entity.</typeparam>
/// <typeparam name="TEntity">The entity type that inherits from IdentityDbModel.</typeparam>
/// <typeparam name="TContext">The database context type that inherits from IdentityDbContext.</typeparam>
public class IdentityBaseRepository<TKeyType, TEntity, TContext> : IIdentityRepository<TKeyType, TEntity>
	where TKeyType : IConvertible
	where TEntity : IdentityDbModel<TKeyType, string>, new()
	where TContext : IdentityDbContext<AppUser, AppRole, string,
		IdentityUserClaim<string>, AppUserRole, IdentityUserLogin<string>,
		IdentityRoleClaim<string>, IdentityUserToken<string>>, new()
{
	/// <summary>
	/// The database context instance used for data operations.
	/// </summary>
	protected internal readonly TContext _context;

	/// <summary>
	/// The current user context for identity-aware operations.
	/// </summary>
	private readonly ICommandContext _currentUser;

	/// <summary>
	/// Initializes a new instance of the IdentityBaseRepository class.
	/// </summary>
	/// <param name="context">The database context to use for operations.</param>
	/// <param name="currentUser">The current user context for identity filtering.</param>
	public IdentityBaseRepository(TContext context, ICommandContext currentUser)
	{
		_context = context ?? new TContext();
		_currentUser = currentUser;
	}

	/// <summary>
	/// Asynchronously adds a new entity to the repository if it doesn't already exist for the current user.
	/// </summary>
	/// <param name="entity">The entity to add.</param>
	/// <returns>True if the entity was added successfully; otherwise, false.</returns>
	public virtual async Task<bool> AddAsync(TEntity? entity)
	{
		if (entity is null)
			return false;

		var doesExist = await _context.Set<TEntity>()
			.AnyAsync(entry => entry.Id.Equals(entity.Id) && entry.UserId.Equals(_currentUser.InstanceId));

		if (doesExist)
			return false;

		_context
			.Set<TEntity>()
			.Add(entity);

		return true;
	}

	/// <summary>
	/// Asynchronously adds a collection of entities to the repository.
	/// </summary>
	/// <param name="entities">The entities to add.</param>
	/// <returns>True if the entities were added successfully; otherwise, false.</returns>
	public virtual Task<bool> AddRangeAsync(IEnumerable<TEntity> entities)
	{
		if (entities is null)
			return Task.FromResult(false);

		_context.Set<TEntity>()
			.AddRange(entities);

		return Task.FromResult(true);
	}

	/// <summary>
	/// Returns a queryable interface for all entities in the repository without identity filtering.
	/// </summary>
	/// <returns>An IQueryable of all entities.</returns>
	public virtual IQueryable<TEntity> AsQueryable()
	{
		return _context.Set<TEntity>()
			.AsQueryable();
	}

	/// <summary>
	/// Returns a queryable interface for entities filtered by the current user's identity.
	/// </summary>
	/// <returns>An IQueryable of entities belonging to the current user.</returns>
	public virtual IQueryable<TEntity> AsIdentityQueryable()
	{
		return _context.Set<TEntity>()
			.Where(cat => cat.UserId == _currentUser.InstanceId)
			.AsQueryable();
	}

	/// <summary>
	/// Asynchronously retrieves an entity by its ID for the current user.
	/// </summary>
	/// <param name="id">The ID of the entity to retrieve.</param>
	/// <returns>The entity if found; otherwise, null.</returns>
	public virtual async Task<TEntity?> GetAsync(TKeyType id)
	{
		return await _context.Set<TEntity>()
			.FirstOrDefaultAsync(entry => entry.Id.Equals(id) && entry.UserId.Equals(_currentUser.InstanceId));
	}

	/// <summary>
	/// Asynchronously removes an entity by its ID for the current user.
	/// </summary>
	/// <param name="id">The ID of the entity to remove.</param>
	/// <returns>True if the entity was removed successfully; otherwise, false.</returns>
	public virtual async Task<bool> RemoveAsync(TKeyType id)
	{
		TEntity entity = new()
		{
			Id = id
		};

		var doesExist = await _context.Set<TEntity>().AnyAsync(entry => entry.Id.Equals(entity.Id) && entry.UserId.Equals(_currentUser.InstanceId));

		if (!doesExist)
			return false;

		_context.Set<TEntity>().Remove(entity);

		return true;
	}

	/// <summary>
	/// Asynchronously removes the specified entity for the current user.
	/// </summary>
	/// <param name="entity">The entity to remove.</param>
	/// <returns>True if the entity was removed successfully; otherwise, false.</returns>
	public virtual async Task<bool> RemoveAsync(TEntity? entity)
	{
		if (entity is null)
			return false;

		var doesExist = await _context.Set<TEntity>().AnyAsync(entry => entry.Id.Equals(entity.Id) && entry.UserId.Equals(_currentUser.InstanceId));
		if (!doesExist)
			return false;

		_context.Set<TEntity>().Remove(entity);

		return true;
	}

	/// <summary>
	/// Asynchronously updates the specified entity in the repository.
	/// </summary>
	/// <param name="entity">The entity to update.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public virtual Task UpdateAsync(TEntity? entity)
	{
		if (entity is null)
			return Task.CompletedTask;

		_context.Set<TEntity>().Update(entity);

		return Task.CompletedTask;
	}

	/// <summary>
	/// Asynchronously updates a collection of entities in the repository.
	/// </summary>
	/// <param name="entities">The entities to update.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public virtual Task UpdateRange(IEnumerable<TEntity> entities)
	{
		_context.Set<TEntity>().UpdateRange(entities);

		return Task.CompletedTask;
	}

	/// <summary>
	/// Asynchronously saves all changes made to the repository.
	/// </summary>
	/// <returns>A task representing the asynchronous save operation.</returns>
	public virtual async Task SaveChangesAsync()
	{
		await _context.SaveChangesAsync();
	}

	/// <summary>
	/// Asynchronously removes a collection of entities from the repository.
	/// </summary>
	/// <param name="entity">The entities to remove.</param>
	/// <returns>True if the entities were removed successfully; otherwise, false.</returns>
	public Task<bool> RemoveRangeAsync(IEnumerable<TEntity> entity)
	{
		if (entity is null)
			return Task.FromResult(true);

		_context
			.Set<TEntity>()
			.RemoveRange(entity);

		return Task.FromResult(true);
	}
}
