using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities;

public class TeacherProfile
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; private set; }

	// Navigation Properties
	[Required]
	public required UserProfile UserProfile { get; init; }

	[Required]
	public required Teacher Teacher { get; init; }
	[Required]
	public int TeacherId { get; set; }

	public static bool operator ==(TeacherProfile? a, TeacherProfile? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.UserProfile == b.UserProfile;
	}

	public static bool operator !=(TeacherProfile? a, TeacherProfile? b) => !( a == b );

	public override bool Equals(object? obj) => obj is TeacherProfile other && this == other;
	public override int GetHashCode() => UserProfile.GetHashCode();
}

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
	[Required]
	public ICollection<Classroom> Classrooms { get; internal set; } = [ ];
	[Required]
	public ICollection<Subject> Subjects { get; internal set; } = [ ];

	public override bool Equals(object? obj) => obj is Teacher other && this == other;

	public static bool operator ==(Teacher? a, Teacher? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.RegisterID == b.RegisterID;
	}

	public static bool operator !=(Teacher? a, Teacher? b) => !( a == b );
	public override int GetHashCode() => HashCode.Combine(RegisterID);
}