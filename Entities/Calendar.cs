using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Util;

namespace Entities;

public class Calendar
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

	// Navigation Properties
	[Required]
	public IEnumerable<CalendarWeek> Data { get; set; } = [ ];
	[Required]
	public int ClassroomId { get; set; }
	[Required]
	public required Classroom Classroom { get; set; }

	public bool TryCompileTeachers()
	{
		var subjectTeachers = Data
			.SelectMany(w => w.Days)
			.SelectMany(d => d.HoursInDay)
			.Select(h => h.Lesson)
			.Select(l => new
			{
				l.Subject,
				l.Teachers
			});

		foreach (var subjectTeacher in subjectTeachers)
		{
			foreach (var teacher in subjectTeacher.Teachers)
			{
				if (!teacher.Subjects.Contains(subjectTeacher.Subject))
				{
					teacher.Subjects.Add(subjectTeacher.Subject);
				}
			}
		}
		throw new NotImplementedException();
	}

}

public class CalendarWeek
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	[Required]
	public required DateTime StartDate { get; set; }
	[NotMapped]
	public DateTime MondayDate { get => StartDate.RoundToMonday(); }
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
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	[Required]
	public required DateTime Date { get; set; }
	[Required]
	public DayOfWeek DayOfWeek { get => Date.DayOfWeek; }
	[Required]
	public required List<HourInDay> HoursInDay { get; set; } = [ ];
	[NotMapped]
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
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	[NotMapped]
	public required bool IsLesson { get; set; }
	[Required]
	public required Lesson Lesson { get; set; }
	[Required]
	public required int Hour { get; set; }
	[Required]
	public required int LinkedHoursCount { get; set; }
	[NotMapped]
	public int Duration { get => LinkedHoursCount + 1; }
}


public class Lesson
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	public required int? RegisterId { get; set; }
	[Required]
	public required int TTCID { get; set; }
	[Required]
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
	public required bool LinkToPreviousHour { get; set; }
}

public class Subject
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	[Required]
	public required int RegisterId { get; set; }
	[Required]
	public required string Name { get; set; }

	public static bool operator ==(Subject? a, Subject? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.RegisterId == b.RegisterId
			&& a.Name == b.Name;
	}

	public static bool operator !=(Subject? a, Subject? b) => !(a == b);
	public override bool Equals(object? obj) => obj is Subject other && this == other;
	public override int GetHashCode() => HashCode.Combine(RegisterId, Name);
}