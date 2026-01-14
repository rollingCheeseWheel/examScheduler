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


public class CalendarDay
{
	public required DateTimeOffset Date { get; set; }
	public required ICollection<Lesson> Lessons { get; set; }
}


public class Lesson : IEquatable<Lesson>
{
	public int? Id { get; set; }
	[Required]
	[JsonConverter(typeof(RegisterDateTimeConverter))]
	public required DateTime Date { get; set; }
	/// <summary>
	/// 1-Indexed
	/// </summary>
	[Required, Range(1, 24), JsonPropertyName("hour")]
	public required int FromHour { get; set; }
	/// <inheritdoc cref="Lesson.FromHour"/>
	[Required, Range(1, 24)]
	public required int ToHour { get; set; }
	[Required, JsonPropertyName("classId")]
	public required int LessonId { get; set; }
	[Required, JsonPropertyName("className")]
	public required string LessonName { get; set; }
	[Required]
	public required ICollection<Teacher> Teachers { get; set; }
	[Required]
	public required Subject Subject { get; set; }

	[Required, JsonConverter(typeof(IntToBoolConverter))]
	public required bool LinkToPreviousHour { get; set; }

	public Lesson() { }

	[JsonConstructor]
	public Lesson(int hour, int toHour)
	{
		FromHour = Math.Clamp(hour, 1, 24);
		ToHour = Math.Clamp(toHour, FromHour, 24);
	}

	public static bool operator ==(Lesson? a, Lesson? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.Date == b.Date
			&& a.FromHour == b.FromHour
			&& a.ToHour == b.ToHour
			&& a.LessonId == b.LessonId
			&& a.LessonName == b.LessonName
			&& a.Subject == b.Subject
			&& a.LinkToPreviousHour == b.LinkToPreviousHour
			&& a.Teachers.ValueEquals(b.Teachers, x => x.Id);
	}
	public static bool operator !=(Lesson? a, Lesson? b) => !( a == b );
	public override bool Equals(object? obj) => obj is Lesson other && Equals(other);
	public bool Equals(Lesson? other) => this == other;
	public override int GetHashCode() => HashCode.Combine(Date, FromHour, ToHour, LessonId, LessonName, Subject, LinkToPreviousHour, Teachers);
}

public class Subject : IEquatable<Subject>
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
	public override bool Equals(object? obj) => obj is Subject other && Equals(other);
	public bool Equals(Subject? other) => this == other;
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