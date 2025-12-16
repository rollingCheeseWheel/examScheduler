using System.ComponentModel.DataAnnotations;
using Util;

namespace Models.API;

public class UserProfile
{
	[Required]
	public required Guid Id { get; set; }
	[Required]
	public required School School { get; init; }
	public Guid SchoolId { get; }
	[Required]
	public required long RegiserId { get; init; }
	[Required]
	public required string FirstName { get; set; }
	[Required]
	public required string LastName { get; set; }
	[Required]
	public required UserRole Role { get; init; }
}

public class StudentProfile
{
	[Required]
	public required UserProfile UserProfile { get; set; }
	[Required]
	public required Classroom Classroom { get; set; }
}

public class TeacherProfile
{
	[Required]
	public required UserProfile UserProfile { get; set; }
	[Required]
	public required IEnumerable<Classroom> Classrooms { get; set; }
}