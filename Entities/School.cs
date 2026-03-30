using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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
	[Required, JsonIgnore]
	public required string Secret { get; set; }
	[Required]
	public required bool IsEnabled { get; set; }

	[Timestamp]
	public uint Version { get; set; }

	public bool Equals(School? other) =>
		SchoolId == other?.SchoolId &&
		IsEnabled == other.IsEnabled &&
		Name == other.Name &&
		RegisterUri == other.RegisterUri &&
		ClientId == other.ClientId &&
		Secret == other.Secret;
	public override bool Equals(object? obj) => obj is School cast && Equals(cast);
	public override int GetHashCode() => HashCode.Combine(SchoolId, IsEnabled, Name, RegisterUri, ClientId, Secret);
	public int CompareTo(School? b) => Name.CompareTo(b?.Name);

	public static bool operator ==(School? a, School? b) => ReferenceEquals(a, b) || ( a is not null && b is not null && a.Equals(b) );
	public static bool operator !=(School? a, School? b) => !( a == b );
}
