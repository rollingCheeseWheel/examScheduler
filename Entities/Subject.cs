using System.ComponentModel.DataAnnotations;

namespace Entities;

public class Subject : EntityBase<Subject>
{
	[Key]
	public override Guid Id { get; set; } = Guid.NewGuid();
	[Required]
	public required string Name { get; set; }
	[Required]
	public required int RegisterId { get; set; }

	public Subject() : base() { }
	public Subject(Models.DigitalesRegister.Subject subject)
	{
		Name = subject.Name;
		RegisterId = subject.Id;
	}

	public bool EqualsModel(Models.DigitalesRegister.Subject? other)
	{
		return other is not null && Name == other.Name;
	}

	public override bool EqualsCore(Subject b)
	{
		return Name == b.Name &&
		RegisterId == b.RegisterId;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Name);
	}

	public override int CompareTo(Subject? other)
	{
		return Name.CompareTo(other?.Name);
	}
}