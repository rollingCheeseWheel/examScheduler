using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities;

public class Teacher
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; private set; }

	[Required]
	public required int RegisterID { get; init; }
	[Required]
	public required string FirstName { get; init; }
	[Required]
	public required string LastName { get; init; }

	public TeacherProfile? TeacherProfile { get; set; }
	public ICollection<Classroom> Classrooms { get; set; } = [ ];
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