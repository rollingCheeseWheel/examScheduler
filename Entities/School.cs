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
	[Required]
	public required bool IsEnabled { get; init; }

	public static bool operator ==(School? a, School? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.Name == b.Name
			&& a.RegisterUri == b.RegisterUri
			&& a.SchoolId == b.SchoolId
			&& a.ClientId == b.ClientId
			&& a.Secret == b.Secret
			&& a.IsEnabled == b.IsEnabled;
	}
	public static bool operator !=(School? a, School? b) => !( a == b );
	public override bool Equals(object? obj) => obj is School other && this == other;
	public override int GetHashCode() => HashCode.Combine(Name, RegisterUri, SchoolId, ClientId, Secret, IsEnabled);

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
