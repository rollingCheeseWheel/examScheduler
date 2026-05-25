using System.ComponentModel.DataAnnotations;
using Util.Extensions;

namespace Entities;

public class Teacher : EntityBase<Teacher>
{
	[Key]
	public override Guid Id { get; set; } = Guid.CreateVersion7();
	[Required]
	public required string Name { get; set; }
	[Required]
	public required string SchoolId { get; set; }

	public Guid? TeacherProfileId { get; set; }
	public TeacherProfile? TeacherProfile { get; set; }

	[Required]
	public ICollection<Subject> Subjects { get; set; } = [ ];
	[Required]
	public ICollection<Classroom> Classrooms { get; set; } = [ ];

	[Timestamp]
	public override uint Version { get; set; }

	public override bool EqualsCore(Teacher b) => Name == b.Name &&
		SchoolId == b.SchoolId &&
		Subjects.ValueEquals(b.Subjects) &&
		TeacherProfile == b.TeacherProfile &&
		Classrooms.ValueEquals(b.Classrooms);

	public override int GetHashCode() => HashCode.Combine(Name, SchoolId, Subjects.GetValueHashCode(), TeacherProfile, Classrooms.GetValueHashCode());

	public override int CompareTo(Teacher? b) => Name.CompareTo(b?.Name);
}