using System.ComponentModel.DataAnnotations;

namespace Entities;

public class StudentProfile : IComparable<StudentProfile>
{
	[Key] // same as userprofile id
	public Guid Id { get; private set; }

	// Navigation Properties
	[Required]
	public required UserProfile UserProfile { get; init; }
	[Required]
	public required Classroom Classroom { get; init; }
	/// <summary>
	/// to convince EF of a many-to-many relationship
	/// a Student has a Classroom, a Classroom has many (indirect) ExamSlots, an ExamSlot has many Students
	/// thus a Student has many ExamSlots
	/// </summary>
	//public ICollection<ExamSlot> ParticipatingExamSlots { get; internal set; } = [ ];
	//public ICollection<ExamSlot> ActuallyParticipatedExamSlots { get; internal set; } = [ ];

	public static bool operator ==(StudentProfile? a, StudentProfile? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.UserProfile == b.UserProfile;
	}
	public static bool operator !=(StudentProfile? a, StudentProfile? b) => !( a == b );
	public override bool Equals(object? obj) => obj is StudentProfile other && this == other;
	public override int GetHashCode() => UserProfile.GetHashCode();
	public int CompareTo(StudentProfile? other) => UserProfile.CompareTo(other?.UserProfile);
}