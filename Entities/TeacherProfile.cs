using System.ComponentModel.DataAnnotations;

namespace Entities;

public class TeacherProfile : EntityBase<TeacherProfile>
{
    [Key]
    public override Guid Id { get; } = Guid.NewGuid();
    [Required]
    public required UserProfile UserProfile { get; set; }

    public Teacher? Teacher { get; set; }
    public Guid? TeacherId { get; private set; }

    public override bool EqualsCore(TeacherProfile b) => UserProfile.Equals(b.UserProfile);
    public override int GetHashCode() => UserProfile.GetHashCode();
    public override int CompareTo(TeacherProfile? other) => UserProfile.CompareTo(other?.UserProfile);
}
