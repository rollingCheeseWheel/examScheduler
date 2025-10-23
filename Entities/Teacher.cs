using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities;

public class Teacher
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

	// Navigation Properties
	[Required]
	public required UserProfile UserProfile { get; set; }


	public Calendar? Timetable { get; set; }
	[Required]
	public ICollection<Classroom> Classrooms { get; set; } = [ ];
	[Required]
	public ICollection<Subject> Subjects { get; set; } = [ ];
	[Required]
	public ICollection<Lesson> Lessons { get; set; } = [ ];

	public override bool Equals(object? obj) => obj is Teacher other && this == other;

	public static bool operator ==(Teacher? a, Teacher?b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.UserProfile == b.UserProfile;
	}

	public static bool operator !=(Teacher? a, Teacher? b) => !(a == b);
	public override int GetHashCode() => UserProfile.GetHashCode();
}
