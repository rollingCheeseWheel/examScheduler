using System.ComponentModel.DataAnnotations;

namespace Entities;

public class TeacherProfile : EntityBase<TeacherProfile>
{
	[Key]
	public override Guid Id { get; set; } = Guid.CreateVersion7();
	[Required]
	public required UserProfile UserProfile { get; set; }

	public Teacher? Teacher { get; set; }
	public Guid? TeacherId { get; private set; }

	[Timestamp]
	public override uint Version { get; set; }

	public override bool EqualsCore(TeacherProfile b) => UserProfile.Equals(b.UserProfile);

	public override int GetHashCode() => HashCode.Combine(UserProfile);

	public override int CompareTo(TeacherProfile? other) => UserProfile.CompareTo(other?.UserProfile);
}
