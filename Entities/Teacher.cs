using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Util.Extensions;

namespace Entities;

public class Teacher : EntityBase<Teacher>
{
	[Key]
	public override Guid Id { get; set; } = Guid.NewGuid();
	[Required]
	public required string FirstName { get; set; }
	[Required]
	public required string LastName { get; set; }
	[NotMapped]
	public string Name => string.Join(" ", FirstName, LastName);
	[Required]
	public Guid SchoolId { get; set; }

	public Guid? TeacherProfileId { get; set; }
	[Required]
	public ICollection<Subject> Subjects { get; set; } = [ ];

	[Timestamp]
	public override uint Version { get; set; }

	public override bool EqualsCore(Teacher b) => Name == b.Name &&
		SchoolId == b.SchoolId &&
		Subjects.ValueEquals(b.Subjects) &&
		TeacherProfileId == b.TeacherProfileId;

	public override int GetHashCode() => HashCode.Combine(Name, SchoolId, Subjects.Order(), TeacherProfileId);

	public override int CompareTo(Teacher? b) => Name.CompareTo(b?.Name);
}