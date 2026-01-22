using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Util;

namespace Entities;

public class Teacher : EntityBase<Teacher>
{
	[Key]
	public override Guid Id { get; set; } = Guid.NewGuid();
	[Required]
	public required int RegisterID { get; set; }
	[Required]
	public required string FirstName { get; set; }
	[Required]
	public required string LastName { get; set; }
	[NotMapped]
	public string Name => string.Join(" ", FirstName, LastName);
	[Required]
	public required School School { get; set; }
	public Guid SchoolId { get; private set; }

	public TeacherProfile? TeacherProfile { get; set; }
	[Required]
	public ICollection<Subject> Subjects { get; set; } = [ ];

	public override bool EqualsCore(Teacher b)
	{
		return Name == b.Name &&
		SchoolId == b.SchoolId &&
		Subjects.ValueEquals(b.Subjects);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Name, SchoolId, Subjects.Order());
	}

	public override int CompareTo(Teacher? b)
	{
		return Name.CompareTo(b?.Name);
	}
}