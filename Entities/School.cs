using System.ComponentModel.DataAnnotations;

namespace Entities;

public class School : EntityBase<School>
{
	[Key]
	public override Guid Id { get; set; } = Guid.NewGuid();
	[Required]
	public required string Name { get; set; }
	[Required]
	public required Uri RegisterUri { get; set; }
	[Required]
	public required string SchoolId { get; set; }
	[Required]
	public required string ClientId { get; set; }
	[Required]
	public required string Secret { get; set; }
	[Required]
	public required bool IsEnabled { get; set; }

	public override bool EqualsCore(School b)
	{
		return Name == b.Name &&
		RegisterUri == b.RegisterUri &&
		SchoolId == b.SchoolId &&
		ClientId == b.ClientId &&
		Secret == b.Secret &&
		IsEnabled == b.IsEnabled;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Name, RegisterUri, SchoolId, ClientId, Secret, IsEnabled);
	}

	public override int CompareTo(School? b)
	{
		return Name.CompareTo(b?.Name);
	}
}
