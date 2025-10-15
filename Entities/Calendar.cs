using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Util;

namespace Entities;

public class Calendar
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

	// Navigation Properties
	[Required]
	public IEnumerable<CalendarWeek> Data { get; set; } = [ ];
	[Required]
	public int ClassroomId { get; set; }
	[Required]
	public required Classroom Classroom { get; set; }
}

public class CalendarWeek
{
	[JsonIgnore]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	[NotMapped]
	[JsonIgnore]
	public bool StartsMonday { get => StartDate.DayOfWeek == DayOfWeek.Monday; }
	[Required]
	public required DateTime StartDate { get; set; }
	[Required]
	public required List<CalendarDay> Days { get; set; } = [ ];

	public IEnumerable<Subject> GetSubjects()
	{
		return Days
			.SelectMany(d => d.GetSubjects())
			.Distinct();
	}

	public IEnumerable<Teacher> GetTeachers()
	{
		return Days
			.SelectMany(d => d.GetTeachers())
			.Distinct();
	}
}

public class CalendarDay
{
	[JsonIgnore]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	[Required]
	public required DateTime Date { get; set; }
	[Required]
	public DayOfWeek DayOfWeek { get => Date.DayOfWeek; }
	[Required]
	public required List<HourInDay> HoursInDay { get; set; } = [ ];
	[NotMapped]
	[JsonIgnore]
	public int TotalHourCount { get => HoursInDay.Select(h => h.Duration).Sum(); }

	public IEnumerable<Subject> GetSubjects()
	{
		return HoursInDay
			.Select(h => h.Lesson.Subject)
			.Distinct();
	}

	public IEnumerable<Teacher> GetTeachers()
	{
		return HoursInDay
			.SelectMany(h => h.Lesson.Teachers)
			.Distinct();
	}
}


public class HourInDay
{
	[JsonIgnore]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	[Required]
	[JsonConverter(typeof(IntToBoolConverter))]
	public required bool IsLesson { get; set; }
	[Required]
	public required Lesson Lesson { get; set; }
	[Required]
	public required int Hour { get; set; }
	[Required]
	public required int LinkedHoursCount { get; set; }
	[NotMapped]
	[JsonIgnore]
	public int Duration { get => LinkedHoursCount + 1; }
}


public class Lesson
{
	[JsonIgnore]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	[JsonPropertyName("id")]
	public required int? RegisterId { get; set; }
	[JsonPropertyName("ttcid")]
	public required int TTCID { get; set; }
	[Required]
	[JsonConverter(typeof(RegisterDateConverter))]
	public required DateTime Date { get; set; }
	[Required]
	public required int Hour { get; set; }
	[Required]
	public required int ToHour { get; set; }
	[Required]
	public required int ClassId { get; set; }
	[Required]
	public required string ClassName { get; set; }
	[Required]
	public required ICollection<Teacher> Teachers { get; set; } = [ ];
	[Required]
	public required Subject Subject { get; set; }
	[Required]

	[JsonConverter(typeof(IntToBoolConverter))]
	public required bool LinkToPreviousHour { get; set; }
}

public class Subject
{
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	[JsonIgnore]
	public int Id { get; set; }
	[Required]
	public required int RegisterId { get; set; }
	[Required]
	[StringLength(255)]
	public required string Name { get; set; }

	public override bool Equals(object? obj)
	{
		if (obj is Subject asSubject)
		{
			return RegisterId == asSubject.RegisterId && Name == asSubject.Name;
		}
		return false;
	}

	public override int GetHashCode() => base.GetHashCode();
}