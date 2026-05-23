using System.ComponentModel.DataAnnotations;

namespace Entities;

public class StudentProfile : EntityBase<StudentProfile>
{
	[Key]
	public override Guid Id { get; set; }
	[Required]
	public required UserProfile UserProfile { get; set; }
	[Required]
	public required Classroom Classroom { get; set; }
	public Guid ClassroomId { get; set; }

	[Timestamp]
	public override uint Version { get; set; }

	public override bool EqualsCore(StudentProfile b) => UserProfile.Equals(b.UserProfile);

	public override int GetHashCode() => UserProfile.GetHashCode();

	public override int CompareTo(StudentProfile? other) => UserProfile.CompareTo(other?.UserProfile);
}