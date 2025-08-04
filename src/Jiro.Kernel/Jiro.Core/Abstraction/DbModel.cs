using System.ComponentModel.DataAnnotations;

namespace Jiro.Core.Abstraction;

/// <summary>
/// Base class for database entities with a generic primary key.
/// </summary>
/// <typeparam name="TKey">The type of the primary key.</typeparam>
public abstract class DbModel<TKey> where TKey : IConvertible
{
	/// <summary>
	/// Gets or sets the primary key for this entity.
	/// </summary>
	[Key]
	public virtual TKey Id { get; set; } = default!;
}
