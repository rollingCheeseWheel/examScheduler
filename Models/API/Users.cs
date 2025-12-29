using System.ComponentModel.DataAnnotations;
using Util;

namespace Models.API;

public class UserProfile
{
	[Required]
	public required Guid Id { get; set; }
	[Required]
	public Guid SchoolId { get; }
	[Required]
	public required string FirstName { get; set; }
	[Required]
	public required string LastName { get; set; }
	[Required]
	public required UserRole Role { get; init; }
}

public class TeacherProfile
{
	[Required]
	public required UserProfile? UserProfile { get; set; }
	[Required]
	public required Teacher? CalendarTeacher { get; set; }
	[Required]
	public required IEnumerable<Subject> Subjects { get; set; }
	[Required]
	public required IEnumerable<Classroom> Classrooms { get; set; }
}