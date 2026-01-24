using System.ComponentModel.DataAnnotations.Schema;

namespace Entities;

[NotMapped]
public abstract class EntityBase<T>
	: IEquatable<T>, IComparable<T>
	where T : EntityBase<T>
{
	public abstract Guid Id { get; set; }

	public abstract bool EqualsCore(T b);
	public abstract override int GetHashCode();
	public virtual int CompareTo(T? b) => Id.CompareTo(b?.Id);

	public bool Equals(T? b) => b is not null && EqualsCore(b);

	public sealed override bool Equals(object? obj) => obj is T asBase && EqualsCore(asBase);

	public static bool operator ==(EntityBase<T>? a, EntityBase<T>? b) => ReferenceEquals(a, b) || ( a is not null && b is not null && a.Equals(b) );
	public static bool operator !=(EntityBase<T>? a, EntityBase<T>? b) => !( a == b );
}
