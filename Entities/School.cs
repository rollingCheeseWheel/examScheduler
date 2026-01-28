using System.ComponentModel.DataAnnotations;

namespace Entities;

public class School : IEquatable<School>, IComparable<School>
{
	[Key]
	public required string SchoolId { get; set; }
	[Required]
	public required string Name { get; set; }
	[Required]
	public required Uri RegisterUri { get; set; }
	[Required]
	public required string ClientId { get; set; }
	[Required]
	public required string Secret { get; set; }
	[Required]
	public required bool IsEnabled { get; set; }

	[Timestamp]
	public uint Version { get; set; }

	public bool Equals(School? other) => SchoolId == other?.SchoolId;
	public override bool Equals(object? obj) => obj is School cast && Equals(cast);
	public override int GetHashCode() => HashCode.Combine(SchoolId);
	public int CompareTo(School? b) => Name.CompareTo(b?.Name);

	public static bool operator ==(School? a, School? b)
	{
		if (ReferenceEquals(a, b)) { return true; }
		if (a is null || b is null) return false;
		return a.Equals(b);
	}
	public static bool operator !=(School? a, School? b) => !( a == b );
}
