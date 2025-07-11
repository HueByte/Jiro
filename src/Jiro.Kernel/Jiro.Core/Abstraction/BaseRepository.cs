using Microsoft.EntityFrameworkCore;

namespace Jiro.Core.Abstraction;

public class BaseRepository<TKeyType, TEntity, TContext> : IRepository<TKeyType, TEntity>
	where TKeyType : IConvertible
	where TEntity : DbModel<TKeyType>, new()
	where TContext : DbContext, new()
{
	protected internal readonly TContext _context;
	public BaseRepository(TContext context)
	{
		_context = context ?? new TContext();
	}

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

	public virtual async Task<bool> AddRangeAsync(IEnumerable<TEntity> entities)
	{
		if (entities is null)
			return false;

		await _context
			.Set<TEntity>()
			.AddRangeAsync(entities);

		return true;
	}

	public virtual IQueryable<TEntity> AsQueryable()
	{
		return _context.Set<TEntity>().AsQueryable();
	}

	public virtual Task<TEntity?> GetAsync(TKeyType id)
	{
		return _context
			.Set<TEntity>()
			.FirstOrDefaultAsync(entry => entry.Id.Equals(id));
	}

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

	public virtual Task UpdateAsync(TEntity? entity)
	{
		if (entity is null)
			return Task.CompletedTask;

		_context
			.Set<TEntity>()
			.Update(entity);

		return Task.CompletedTask;
	}

	public virtual Task UpdateRange(IEnumerable<TEntity> entities)
	{
		_context
			.Set<TEntity>()
			.UpdateRange(entities);

		return Task.CompletedTask;
	}

	public virtual Task SaveChangesAsync()
	{
		return _context.SaveChangesAsync();
	}

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
