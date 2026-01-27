using System.ComponentModel.DataAnnotations;
using Util.Extensions;

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

	[Timestamp]
	public override uint Version { get; set; }

	public override bool EqualsCore(TeacherProfile b) => UserProfile.Equals(b.UserProfile) &&
		Classrooms.ValueEquals(b.Classrooms);

	public override int GetHashCode() => HashCode.Combine(UserProfile, Classrooms.Order());

	public override int CompareTo(TeacherProfile? other) => UserProfile.CompareTo(other?.UserProfile);
}
