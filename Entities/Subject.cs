using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities;

public class Subject : IComparable<Subject>
{
	[Key]
	public Guid Id { get; private set; } = Guid.NewGuid();
	[Required]
	public required int RegisterId { get; init; }
	[Required]
	public required string Name { get; init; }

	public static implicit operator Subject(Models.DigitalesRegister.Subject subject)
	{
		return new Subject { Name = subject.Name, RegisterId = subject.Id };
	}

	public static bool operator ==(Subject? a, Subject? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.Name == b.Name;
	}
	public static bool operator !=(Subject? a, Subject? b) => !( a == b );
	public override bool Equals(object? obj) => obj is Subject other && this == other;
	public override int GetHashCode() => HashCode.Combine(Name);
	public int CompareTo(Subject? other)
	{
		if (other is null) { return 1; }
		var res = Name.CompareTo(other.Name);
		if (res != 0) { return res; }
		return Id.CompareTo(other.Id);
	}
}