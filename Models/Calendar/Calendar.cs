using System;
using System.Text.Json.Serialization;

namespace Models.Calendar;

public class CalendarDay
{
	public required DateTime Date { get; set; }
	public required List<HourInDay> HoursInDay { get; set; } = [ ];
}


public class HourInDay
{
	public required int IsLesson { get; set; }
	public required Lesson Lesson { get; set; }
	public required int Hour { get; set; }
	public required int LinkedHoursCount { get; set; }
}

public class Lesson
{
	public required int Id { get; set; }
	[JsonPropertyName("ttcid")]
	public required int TTCID { get; set; }
	public required string Date { get; set; }
	public required int Hour { get; set; }
	public required int ToHour { get; set; }
	public required int ClassId { get; set; }
	public required string ClassName { get; set; }
	public required Teacher[ ] Teachers { get; set; }
	public required Subject Subject { get; set; }
	public required int LinkToPreviousHour { get; set; }
}

public class Subject
{
	public required int Id { get; set; }
	public required string Name { get; set; }
	public required int Lernfeld { get; set; }
	public required string DefaultLessonContent { get; set; }
	public required int DefaultLessonContentType { get; set; }
}

public class Teacher
{
	public required int Id { get; set; }
	public required string FirstName { get; set; }
	public required string LastName { get; set; }
}
