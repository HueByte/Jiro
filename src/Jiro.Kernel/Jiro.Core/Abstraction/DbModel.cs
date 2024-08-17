using System.ComponentModel.DataAnnotations;

namespace Jiro.Core.Abstraction;

public abstract class DbModel<TKey> where TKey : IConvertible
{
    [Key]
    public virtual TKey Id { get; set; } = default!;
}