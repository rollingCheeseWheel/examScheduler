using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Util;

namespace Entities;

public class TeacherProfile : EntityBase<TeacherProfile>
{
	[Key]
	public override Guid Id { get; set; } = Guid.NewGuid();
	[Required]
	public required UserProfile UserProfile { get; set; }
	[Required, NotNull]
	public ICollection<Classroom> Classrooms { get; set; } = [ ];

	public Teacher? Teacher { get; set; }
	public Guid? TeacherId { get; private set; }

	public override bool EqualsCore(TeacherProfile b) => UserProfile.Equals(b.UserProfile) &&
		Classrooms.ValueEquals(b.Classrooms);

	public override int GetHashCode() => HashCode.Combine(UserProfile, Classrooms.Order());

	public override int CompareTo(TeacherProfile? other) => UserProfile.CompareTo(other?.UserProfile);
}
