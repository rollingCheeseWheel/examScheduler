using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Util.Validation;

namespace Models.API;

public class Lesson
{
	[Required]
	public required Guid Id { get; set; }
	[Required, DefinedEnum]
	public required DayOfWeek DayOfWeek { get; set; }
	[Required, MinValue(0)]
	public required int FromHour { get; set; }
	[Required, GreaterThan<int>(nameof(FromHour))]
	public required int ToHour { get; set; }
	[Required]
	public required string LessonName { get; set; }
	[Required]
	public required IEnumerable<Teacher> Teachers { get; set; }
	[Required]
	public required Subject Subject { get; set; }
}

public class Teacher()
{
	[Required]
	public required string Name { get; set; }

	[SetsRequiredMembers]
	public Teacher(string name) : this() => Name = name;
}

public class Subject()
{
	[Required]
	public required string Name { get; set; }

	[SetsRequiredMembers]
	public Subject(string name) : this() => Name = name;
}