using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities;
public class School : IComparable<School>
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; private set; }
	[Required]
	public required string Name { get; init; }
	[Required]
	public required Uri RegisterUri { get; init; }
	[Required]
	public required string SchoolId { get; init; }
	[Required]
	public required string ClientID { get; init; }

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
		if (other is null) return 1;

		int c;

		c = string.Compare(Name, other.Name, StringComparison.Ordinal);
		if (c is not 0) return c;

		c = string.Compare(SchoolId, other.SchoolId, StringComparison.Ordinal);
		if (c is not 0) return c;

		c = string.Compare(RegisterUri.ToString(), other.RegisterUri.ToString(), StringComparison.Ordinal);
		if (c is not 0) return c;

		return 0;
	}
}
