using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Models.DigitalesRegister;
using Util;

namespace Entities;

public class CalendarWeek
{
	[NotMapped]
	[JsonIgnore]
	public bool StartsMonday { get => StartDate.DayOfWeek == DayOfWeek.Monday; }
	public required DateTime StartDate { get; set; }
	public required List<CalendarDay> Days { get; set; }

	public IEnumerable<Subject> GetSubjects()
	{
		return Days
			.SelectMany(d => d.GetSubjects())
			.Distinct();
	}

	public IEnumerable<Models.DigitalesRegister.Teacher> GetTeachers()
	{
		return Days
			.SelectMany(d => d.GetTeachers())
			.Distinct();
	}
}

public class CalendarDay
{
	public required DateTime Date { get; set; }
	public DayOfWeek DayOfWeek { get => Date.DayOfWeek; }
	public required List<HourInDay> HoursInDay { get; set; } = [ ];
	[NotMapped]
	[JsonIgnore]
	public int TotalHourCount { get => HoursInDay.Select(h => h.Duration).Aggregate((p, c) => p + c); }

	public IEnumerable<Subject> GetSubjects()
	{
		return HoursInDay
			.Select(h => h.Lesson.Subject)
			.Distinct();
	}

	public IEnumerable<Models.DigitalesRegister.Teacher> GetTeachers()
	{
		return HoursInDay
			.SelectMany(h => h.Lesson.Teachers)
			.Distinct();
	}
}


public class HourInDay
{
	[JsonConverter(typeof(IntToBoolConverter))]
	public required bool IsLesson { get; set; }
	public required Lesson Lesson { get; set; }
	public required int Hour { get; set; }
	public required int LinkedHoursCount { get; set; }
	[NotMapped]
	[JsonIgnore]
	public int Duration { get => LinkedHoursCount + 1; }
}
