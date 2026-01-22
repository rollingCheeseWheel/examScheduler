using System.ComponentModel.DataAnnotations;

namespace Entities;

public class StudentProfile : EntityBase<StudentProfile>
{
	[Key]
	public override Guid Id { get; set; } = Guid.NewGuid();
	[Required]
	public required UserProfile UserProfile { get; set; }
	[Required]
	public required Classroom Classroom { get; set; }

	public override bool EqualsCore(StudentProfile b)
	{
		return UserProfile.Equals(b.UserProfile);
	}

	public override int GetHashCode()
	{
		return UserProfile.GetHashCode();
	}

	public override int CompareTo(StudentProfile? other)
	{
		return UserProfile.CompareTo(other?.UserProfile);
	}
}