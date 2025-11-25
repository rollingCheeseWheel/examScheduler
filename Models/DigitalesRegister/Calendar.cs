using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Util.Converters;
using Util;

namespace Models.DigitalesRegister;

public class CalendarRequest
{
	[JsonConverter(typeof(RegisterDateTimeConverter))]
	public DateTime StartDate { get; set; }
}

public record TeacherSubjects(Teacher Teacher, IEnumerable<Subject> Subjects);
public record DetailedTeacherSubjects(Teacher Teacher, IEnumerable<DetailedSubject> Subjects);
public record DetailedSubject(Subject Subject, int Count);

public class Calendar(ICollection<CalendarDay> days)
{
	public required ICollection<CalendarDay> Days { get; set; } = days;

	/*/// <summary>
	/// Get an IEnumerable of Teachers, each Teacher has an additional IEnumerable of Subjects they are associated with.
	/// This function also filters out noise like substitute teachers, altough this only works (well) when the calendar is long(er)
	/// </summary>
	/// <param name="ignorePercentage"></param>
	/// <returns></returns>
	public IEnumerable<TeacherSubjects> CompileTeachersWithSubjects(double ignorePercentage = 0.25)
	{
		var summedTeacherSubjects = Days
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
	}*/

	public (int ClassroomId, string Name)? GetClassroomInfo()
	{
		return Days
			.SelectMany(d => d.Lessons)
			.Select(l => (ClassroomId: l.ClassId, Name: l.ClassName))
			.GroupBy(l => l.ClassroomId)
			.Select(group => (Value: group.FirstOrDefault(), Count: group.Count()))
			.OrderByDescending(x => x.Count)
			.Select(x => x.Value)
			.FirstOrDefault();
	}
}

public class CalendarDay
{
	public required DateTimeOffset Date { get; set; }
	public required ICollection<Lesson> Lessons { get; set; }
}


public class Lesson
{
	public int? Id { get; set; }
	[Required]
	[JsonConverter(typeof(RegisterDateTimeConverter))]
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
	public required ICollection<Teacher> Teachers { get; set; }
	[Required]
	public required Subject Subject { get; set; }

	[JsonConverter(typeof(IntToBoolConverter))]
	public required bool LinkToPreviousHour { get; set; }


	public static bool operator ==(Lesson? a, Lesson? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.Date == b.Date
			&& a.Hour == b.Hour
			&& a.ToHour == b.ToHour
			&& a.ClassId == b.ClassId
			&& a.ClassName == b.ClassName
			&& a.Subject == b.Subject
			&& a.LinkToPreviousHour == b.LinkToPreviousHour
			&& a.Teachers.SequenceEqual(b.Teachers, x => x.Id);
	}
	public static bool operator !=(Lesson? a, Lesson? b) => !( a == b );
	public override bool Equals(object? obj) => obj is Lesson other && this == other;
	public override int GetHashCode() => HashCode.Combine(Date, Hour, ToHour, ClassId, ClassName, Subject, LinkToPreviousHour, Teachers);
}

public class Subject
{
	[Required]
	public required int Id { get; set; }
	[Required]
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
	[Required]
	public required int Id { get; set; }
	[Required]
	public required string FirstName { get; set; }
	[Required]
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