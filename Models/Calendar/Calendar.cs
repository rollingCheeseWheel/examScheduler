using System.Text.Json.Serialization;
using Util;

namespace Models.Calendar;

public class CalendarWeek
{
	[JsonIgnore]
	public bool StartsMonday { get => StartDate.DayOfWeek == DayOfWeek.Monday; }
	public required DateTime StartDate { get; set; }
	public required List<CalendarDay> Days { get; set; }
}

public class CalendarDay
{
	public required DateTime Date { get; set; }
	public DayOfWeek DayOfWeek { get => Date.DayOfWeek; }
	public required List<HourInDay> HoursInDay { get; set; } = [ ];
	[JsonIgnore]
	public int TotalHourCount { get => HoursInDay.Select(h => h.Duration).Aggregate((p, c) => p + c); }
}


public class HourInDay
{
	[JsonConverter(typeof(IntToBoolConverter))]
	public required bool IsLesson { get; set; }
	public required Lesson Lesson { get; set; }
	public required int Hour { get; set; }
	public required int LinkedHoursCount { get; set; }
	[JsonIgnore]
	public int Duration { get => LinkedHoursCount + 1; }
}

public class Lesson
{
	public required int? Id { get; set; }
	[JsonPropertyName("ttcid")]
	public required int TTCID { get; set; }
	[JsonConverter(typeof(RegisterDateConverter))]
	public required DateTime Date { get; set; }
	public required int Hour { get; set; }
	public required int ToHour { get; set; }
	public required int ClassId { get; set; }
	public required string ClassName { get; set; }
	public required Teacher[ ] Teachers { get; set; }
	public required Subject Subject { get; set; }

	[JsonConverter(typeof(IntToBoolConverter))]
	public required bool LinkToPreviousHour { get; set; }
}

public class Subject
{
	public required int Id { get; set; }
	public required string Name { get; set; }
}

public class Teacher
{
	public required int Id { get; set; }
	public required string FirstName { get; set; }
	public required string LastName { get; set; }
}
