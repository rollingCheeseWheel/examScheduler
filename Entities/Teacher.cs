using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities;

public class Teacher
{
	[Key]
	public Guid Id { get; set; }

	[Required]
	public required int RegisterID { get; init; }
	[Required]
	public required string FirstName { get; init; }
	[Required]
	public required string LastName { get; init; }
	[Required]
	public required School School { get; init; }
	public Guid SchoolId { get; init; }

	public TeacherProfile? TeacherProfile { get; set; }
	[Required]
	public ICollection<Classroom> Classrooms { get; set; } = [ ];
	[Required]
	public ICollection<Subject> Subjects { get; set; } = [ ];

	public static bool operator ==(Teacher? a, Teacher? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.RegisterID == b.RegisterID
			&& a.FirstName == b.FirstName
			&& a.LastName == b.LastName;
	}
	public static bool operator !=(Teacher? a, Teacher? b) => !( a == b );
	public override bool Equals(object? obj) => obj is Teacher other && this == other;
	public override int GetHashCode() => HashCode.Combine(RegisterID);
}