namespace Jiro.Core.Abstraction;

public class IdentityDbModel<TKey, TUserKey> : DbModel<TKey>
    where TKey : IConvertible
    where TUserKey : IConvertible
{
    public virtual TUserKey UserId { get; set; } = default!;
}