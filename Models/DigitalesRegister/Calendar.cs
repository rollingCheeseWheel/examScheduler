using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Util;

namespace Models.DigitalesRegister;

public class CalendarRequest
{
	[JsonConverter(typeof(RegisterDateConverter))]
	public DateTime StartDate { get; set; }
}

public record TeacherSubjects(Teacher Teacher, ICollection<Subject> Subjects);

public class Calendar
{
	public ICollection<CalendarWeek> Data { get; set; } = [ ];

	[Obsolete("TODO: make this weekly to filter out noise like subjstitue teachers")]
	public ICollection<TeacherSubjects> CompileTeachersWithSubject()
	{
		return Data
			.SelectMany(w => w.Days)
			.SelectMany(d => d.HoursInDay)
			.Select(h => h.Lesson) // Lessons
			.SelectMany(l => l.Teachers.Select(t => new
			{
				Teacher = t,
				l.Subject
			}))
			.GroupBy(x => x.Teacher) // make distinct 
			.Select(g => new TeacherSubjects
			(
				g.Key, // Distinct Teachers
				g.Select(x => x.Subject).Distinct().ToList() // Distinct Subjects
			))
			.ToList();
	}
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

	public static bool operator ==(Subject? a, Subject? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.Id == b.Id && a.Name == b.Name;
	}

	public static bool operator !=(Subject? a, Subject? b) => !( a == b );
	public override bool Equals(object? obj) => obj is Subject other && this == other;
	public override int GetHashCode() => HashCode.Combine(Id, Name);
}

public class Teacher
{
	public required int Id { get; set; }
	public required string FirstName { get; set; }
	public required string LastName { get; set; }

	public static bool operator ==(Teacher? a, Teacher? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.FirstName == b.FirstName
			&& a.LastName == b.LastName
			&& a.Id == b.Id;
	}

	public static bool operator !=(Teacher? a, Teacher? b) => !( a == b );
	public override bool Equals(object? obj) => obj is Teacher other && this == other;
	public override int GetHashCode() => HashCode.Combine(Id, FirstName, LastName);
}