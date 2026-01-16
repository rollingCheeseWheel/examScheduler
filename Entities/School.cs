using System.ComponentModel.DataAnnotations;

namespace Entities;
public class School : EntityBase<School>
{
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

	public override bool EqualsCore(School b) =>
		Name == b.Name &&
		RegisterUri == b.RegisterUri &&
		SchoolId == b.SchoolId &&
		ClientId == b.ClientId &&
		Secret == b.Secret &&
		IsEnabled == b.IsEnabled;
	public override int GetHashCode() => HashCode.Combine(Name, RegisterUri, SchoolId, ClientId, Secret, IsEnabled);
	public override int CompareTo(School? b) => Name.CompareTo(b?.Name);
}
