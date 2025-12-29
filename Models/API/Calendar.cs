using System.ComponentModel.DataAnnotations;

namespace Models.API;

public class Calendar
{
	[Required]
	public required Guid Id { get; set; }
	[Required]
	public required Guid ClassroomId { get; set; }
	[Required]
	public required DateTimeOffset LastsUntil { get; set; }
	[Required]
	public required IEnumerable<Lesson> Lessons { get; set; }

}

public class Lesson
{
	[Required]
	public required Guid Id { get; set; }
	[Required]
	public required IEnumerable<DateTimeOffset> Occurances { get; set; }
	[Required]
	public required int FromHour { get; set; }
	[Required]
	public required int ToHour { get; set; }
	[Required]
	public required string LessonName { get; set; }
	[Required]
	public required IEnumerable<Teacher> Teachers { get; set; }
	[Required]
	public required Subject Subject { get; set; }
}

public class Teacher
{
	[Required]
	public required string FirstName { get; set; }
	[Required]
	public required string LastName { get; set; }
}

public class Subject
{
	[Required]
	public required string Name { get; set; }
}