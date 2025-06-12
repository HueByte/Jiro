namespace Jiro.Core.Abstraction;

public interface IIdentityRepository<Tkey, TEntity> : IRepository<Tkey, TEntity>
	where Tkey : IConvertible
	where TEntity : IdentityDbModel<Tkey, string>
{
	IQueryable<TEntity> AsIdentityQueryable ();
}
