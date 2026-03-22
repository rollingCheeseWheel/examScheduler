using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Util.Validation;

namespace Models.API;

public class Lesson
{
	[Required]
	public required Guid Id { get; set; }
	[Required]
	public required DateOnly Date { get; set; }
	[Required, MinValue(0)]
	public required int FromHour { get; set; }
	[Required, GreaterThan<int>(nameof(FromHour))]
	public required int ToHour { get; set; }
	[Required]
	public required string LessonName { get; set; }
	[Required]
	public required IEnumerable<TeacherOnly> Teachers { get; set; }
	[Required]
	public required Subject Subject { get; set; }
}

public class TeacherOnly
{
	[Required]
	public required string Name { get; set; }
}

public class TeacherWithSubjects
{
	[Required]
	public required string Name { get; set; }
	[Required]
	public required IEnumerable<Subject> Subjects { get; set; }
}

public class Subject()
{
	[Required]
	public required string Name { get; set; }

	[SetsRequiredMembers]
	public Subject(string name) : this() => Name = name;
}