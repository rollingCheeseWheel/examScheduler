using System.Text.Json.Serialization;
using Util;

namespace Models.DigitalesRegister;

public class CalendarRequest
{
	[JsonConverter(typeof(RegisterDateConverter))]
	public DateTime StartDate { get; set; }
}

public class Calendar
{
	public ICollection<CalendarWeek> Data { get; set; } = [ ];
}

public class CalendarWeek
{
	public DateTime StartDate { get => Days.Select(d => d.Date).Order().FirstOrDefault(); }
	public required ICollection<CalendarDay> Days { get; set; } = [ ];
}

public class CalendarDay
{
	public required DateTime Date { get; set; }
	public required ICollection<HourInDay> HoursInDay { get; set; } = [ ];
}


public class HourInDay
{
	[JsonConverter(typeof(IntToBoolConverter))]
	public required bool IsLesson { get; set; }
	public required Lesson Lesson { get; set; }
	public required int Hour { get; set; }
	public required int LinkedHoursCount { get; set; }
}


public class Lesson
{
	public required int? Id { get; set; }
	[JsonConverter(typeof(RegisterDateConverter))]
	public required DateTime Date { get; set; }
	public required int Hour { get; set; }
	public required int ToHour { get; set; }
	public required int ClassId { get; set; }
	public required string ClassName { get; set; }
	public required ICollection<Teacher> Teachers { get; set; } = [ ];
	public required Subject Subject { get; set; }

	[JsonConverter(typeof(IntToBoolConverter))]
	public required bool LinkToPreviousHour { get; set; }
}

public class Subject
{
	public required int Id { get; set; }
	public required string Name { get; set; }

	public override int GetHashCode() => base.GetHashCode();

	public override bool Equals(object? obj) => obj is Subject other && this == other;

	public static bool operator ==(Subject? a , Subject? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.Id == b.Id && a.Name == b.Name;
	}

	public static bool operator !=(Subject? a , Subject? b) => !(a == b);
}

public class Teacher
{
	public required int Id { get; set; }
	public required string FirstName { get; set; }
	public required string LastName { get; set; }

	public override int GetHashCode() => base.GetHashCode();

	public override bool Equals(object? obj) => obj is Teacher other && this == other;

	public static bool operator ==(Teacher? a, Teacher? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.FirstName == b.FirstName
			&& a.LastName == b.LastName
			&& a.Id == b.Id;
	}

	public static bool operator !=(Teacher? a, Teacher? b) => !(a == b);
}