using System.ComponentModel.DataAnnotations;

namespace Entities;

public abstract class EntityBase<T> 
	: IEquatable<T>, IComparable<T>
	where T : EntityBase<T>
{
	[Key]
	public Guid Id { get; private set; } = Guid.NewGuid();

	public abstract bool EqualsCore(T b);
	public abstract override int GetHashCode();
	public virtual int CompareTo(T? b) => Id.CompareTo(b?.Id);

	public bool Equals(T? b) => b is not null && EqualsCore(b);
	public sealed override bool Equals(object? obj) => obj is T asBase && Equals(asBase);
    public static bool operator ==(EntityBase<T>? a, EntityBase<T>? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.Equals(b);
	}
	public static bool operator !=(EntityBase<T>? a, EntityBase<T>? b) => !( a == b );
}
