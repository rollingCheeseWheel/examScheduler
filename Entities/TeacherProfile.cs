using System.ComponentModel.DataAnnotations;
using Util;

namespace Entities;

public class TeacherProfile : EntityBase<TeacherProfile>
{
	[Key]
	public override Guid Id { get; set; } = Guid.NewGuid();
	[Required]
	public required UserProfile UserProfile { get; set; }
	[Required]
	public ICollection<Classroom> Classrooms { get; set; } = [ ];

	public Teacher? Teacher { get; set; }
	public Guid? TeacherId { get; private set; }

	public override bool EqualsCore(TeacherProfile b)
	{
		return UserProfile.Equals(b.UserProfile) &&
		Classrooms.ValueEquals(b.Classrooms);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(UserProfile, Classrooms.Order());
	}

	public override int CompareTo(TeacherProfile? other)
	{
		return UserProfile.CompareTo(other?.UserProfile);
	}
}
