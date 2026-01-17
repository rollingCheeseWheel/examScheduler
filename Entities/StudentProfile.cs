using System.ComponentModel.DataAnnotations;

namespace Entities;

public class StudentProfile : EntityBase<StudentProfile>
{
    [Key]
    public override Guid Id { get; } = Guid.NewGuid();
    [Required]
    public required UserProfile UserProfile { get; init; }
    [Required]
    public required Classroom Classroom { get; init; }
    /// <summary>
    /// to convince EF of a many-to-many relationship
    /// a Student has a Classroom, a Classroom has many (indirect) ExamSlots, an ExamSlot has many Students
    /// thus a Student has many ExamSlots
    /// </summary>
    public ICollection<ExamSlot> ParticipatingExamSlots { get; internal set; } = [ ];
    public ICollection<ExamSlot> ActuallyParticipatedExamSlots { get; internal set; } = [ ];

    public override bool EqualsCore(StudentProfile b) => UserProfile.Equals(b.UserProfile);
    public override int GetHashCode() => UserProfile.GetHashCode();
    public override int CompareTo(StudentProfile? other) => UserProfile.CompareTo(other?.UserProfile);
}