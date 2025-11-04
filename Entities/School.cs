using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities;
public class School
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; private set; }
	[Required]
	public required string Name { get; init; }
	[Required]
	public required Uri RegisterUri { get; init; }

	public static bool operator ==(School? a, School? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.RegisterUri == b.RegisterUri
			&& a.Name == b.Name;
	}
	public static bool operator !=(School? a, School? b) => !( a == b );
	public override bool Equals(object? obj) => obj is School other && this == other;
	public override int GetHashCode() => HashCode.Combine(RegisterUri, Name);
}
