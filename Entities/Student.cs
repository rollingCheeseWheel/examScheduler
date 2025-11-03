using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models.DigitalesRegister;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities;

public class Student
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; }

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
	[Required]
	public ICollection<ExamSlot> ParticipatingExamSlots { get; internal set; } = [ ];
	[Required]
	public ICollection<ExamSlot> ActuallyParticipatedExamSlots { get; internal set; } = [ ];

	public static bool operator ==(Student? a, Student? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.UserProfile == b.UserProfile;
	}
	public static bool operator !=(Student? a, Student? b) => !( a == b );
	public override bool Equals(object? obj) => obj is Student other && this == other;
	public override int GetHashCode() => UserProfile.GetHashCode();
}