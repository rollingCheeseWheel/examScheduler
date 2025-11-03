using System.Text.Json.Serialization;
using Util;

namespace Models.DigitalesRegister;

public class CalendarRequest
{
	[JsonConverter(typeof(RegisterDateConverter))]
	public DateTimeOffset StartDate { get; set; }
}

public record TeacherSubjects(Teacher Teacher, IEnumerable<Subject> Subjects);
public record DetailedTeacherSubjects(Teacher Teacher, IEnumerable<DetailedSubject> Subjects);
public record DetailedSubject(Subject Subject, int Count);


public class Calendar
{
	public Calendar(ICollection<CalendarWeek> weeks) => Data = weeks;

	public ICollection<CalendarWeek> Data { get; set; } = [ ];

	/// <summary>
	/// Get an IEnumerable of Teachers, each Teacher has an additional IEnumerable of Subjects they are associated with.
	/// This function also filters out noise like substitute teachers, altough this only works (well) when the calendar is long(er)
	/// </summary>
	/// <param name="ignorePercentage"></param>
	/// <returns></returns>
	public IEnumerable<TeacherSubjects> CompileTeachersWithSubjects(double ignorePercentage = 0.25)
	{
		var summedTeacherSubjects = Data
			.SelectMany(w => w.CompileTeachersWithSubjects()) // get a list of all TeacherSubjects side-by-side
			.GroupBy(x => x.Teacher) // group teachers together, is less efficient but more useful
			.Select(g => new DetailedTeacherSubjects( // reform a single TeacherSubject with the summed up lesson counts
				g.Key,
				g.SelectMany(a => a.Subjects)
				.GroupBy(x => x.Subject)
				.Select(a => new DetailedSubject( // sum up lesson counts
					a.Key,
					a.Sum(c => c.Count)
				))
			));

		var ignoreThreshold = summedTeacherSubjects
			.SelectMany(t => t.Subjects)
			.GroupBy(x => x.Subject)
			.Select(g => g.Sum(s => s.Count))
			.Average() * ignorePercentage;

		return summedTeacherSubjects
			.Select(t => new TeacherSubjects(
				t.Teacher,
				t.Subjects.Where(s => s.Count > ignoreThreshold).Select(s => s.Subject)
			))
			.Where(t => t.Subjects.Any());
	}
}

public class CalendarWeek
{
	public DateTimeOffset StartDate { get => Days.Select(d => d.Date).Order().FirstOrDefault(); }
	public required ICollection<CalendarDay> Days { get; set; } = [ ];

	internal IEnumerable<DetailedTeacherSubjects> CompileTeachersWithSubjects()
	{
		return Days
			.SelectMany(d => d.HoursInDay)
			.Select(h => h.Lesson) // Lessons
			.SelectMany(l => l.Teachers.Select(t => new
			{
				Teacher = t,
				l.Subject
			}))
			.GroupBy(x => x.Teacher) // make distinct 
			.Select(g => new DetailedTeacherSubjects
			(
				g.Key, // Distinct Teachers
				g.GroupBy(x => x.Subject).Select(a => new DetailedSubject(a.Key, a.Count())) // Distinct Subjects
			));
	}
}

public class CalendarDay
{
	public required DateTimeOffset Date { get; set; }
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
	public required DateTimeOffset Date { get; set; }
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