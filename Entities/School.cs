using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities;
public class School : IComparable<School>
{
	[Key]
	public Guid Id { get; private set; } = Guid.NewGuid();
	[Required]
	public required string Name { get; init; }
	[Required]
	public required Uri RegisterUri { get; init; }
	[Required]
	public required string SchoolId { get; init; }
	[Required]
	public required string ClientId { get; init; }
	[Required]
	public required string Secret { get; init; }

	public static bool operator ==(School? a, School? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.RegisterUri == b.RegisterUri
			&& a.Name == b.Name
			&& a.SchoolId == b.SchoolId;
	}
	public static bool operator !=(School? a, School? b) => !( a == b );
	public override bool Equals(object? obj) => obj is School other && this == other;
	public override int GetHashCode() => HashCode.Combine(RegisterUri, Name, SchoolId);

	public int CompareTo(School? other)
	{
		if (other is null) { return 1; }
		var res = Name.CompareTo(other.Name);
		if (res != 0) { return res; }
		res = RegisterUri.AbsoluteUri.CompareTo(other.RegisterUri.AbsoluteUri);
		if (res != 0) { return res; }
		return Id.CompareTo(other.Id);
	}
}
