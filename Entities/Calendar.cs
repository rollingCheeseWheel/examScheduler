using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Util;

namespace Entities;

public class Calendar
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; private set; }

	[Required, Range(0, int.MaxValue)]
	public int TimesScanned { get; private set; } = 1;
	[Required]
	public DateTimeOffset LastScanned { get; private set; } = DateTimeOffset.UtcNow;

	[Required]
	public required ICollection<CalendarDay> Days { get; set; } = [ ];
	public Classroom? Classroom { get; set; }

	public static Calendar Parse(Classroom classroom, Models.DigitalesRegister.Calendar calendar, ICollection<Teacher> teachers) => Parse(classroom, calendar.Days, teachers);

	public static Calendar Parse(Classroom classroom, ICollection<Models.DigitalesRegister.CalendarDay> days, ICollection<Teacher> teachers)
	{
		return new()
		{
			Days = days.Select(d => CalendarDay.Parse(d, teachers)).ToList(),
			Classroom = classroom
		};
	}

	public static bool operator ==(Calendar? a, Calendar? d)
	{
		if (ReferenceEquals(a, d)) return true;
		if (a is null || d is null) return false;
		return a.Classroom == d.Classroom
			&& a.Days.SequenceEqual(d.Days, x => x.Date);
	}
	public static bool operator !=(Calendar? a, Calendar? b) => !( a == b );
	public override bool Equals(object? obj) => obj is Calendar other && this == other;
	public override int GetHashCode() => HashCode.Combine(Classroom, Days.OrderBy(d => d.Date));
}

public class CalendarDay
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; private set; }
	[Required]
	public required DateTimeOffset Date { get; init; }
	public DayOfWeek DayOfWeek { get => Date.DayOfWeek; }
	public List<Lesson> Lessons { get; set; } = [ ];

	public static CalendarDay Parse(Models.DigitalesRegister.CalendarDay calendarDay, ICollection<Teacher> teachers)
	{
		return new()
		{
			Date = calendarDay.Date.ToUniversalTime(),
			Lessons = calendarDay.Lessons.Select(l => Lesson.Parse(l, teachers)).ToList(),
		};
	}

	public static bool operator ==(CalendarDay? a, CalendarDay? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.Date == b.Date
			&& a.Lessons.SequenceEqual(b.Lessons, h => h.Hour);
	}
	public static bool operator !=(CalendarDay? a, CalendarDay? b) => !( a == b );
	public override bool Equals(object? obj) => obj is CalendarDay other && this == other;
	public override int GetHashCode() => HashCode.Combine(Date, Lessons.OrderBy(h => h.Hour));
}

[Obsolete]
public class HourInDay
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; private set; }
	[Required]
	public required Lesson Lesson { get; init; }
	[Required]
	public required int Hour { get; init; }
	[Required]
	public required int LinkedHoursCount { get; init; }
	[NotMapped]
	public int Duration { get => LinkedHoursCount + 1; }

	public static bool operator ==(HourInDay? a, HourInDay? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.Lesson == b.Lesson
			&& a.Hour == b.Hour
			&& a.LinkedHoursCount == b.LinkedHoursCount;
	}
	public static bool operator !=(HourInDay? a, HourInDay? b) => !( a == b );
	public override bool Equals(object? obj) => obj is HourInDay other && this == other;
	public override int GetHashCode() => HashCode.Combine(Lesson, Hour, LinkedHoursCount);
}

public class Lesson
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; private set; }
	public required int? RegisterId { get; set; }
	[Required]
	public required DateTimeOffset Date { get; set; }
	[Required]
	public required int Hour { get; set; }
	[Required]
	public required int ToHour { get; set; }
	[Required]
	public required int ClassId { get; set; }
	[Required]
	public required string ClassName { get; set; }
	[Required]
	public required bool LinkToPreviousHour { get; set; }

	public required ICollection<Teacher> Teachers { get; set; } = [ ];
	public required Subject Subject { get; set; }

	public static Lesson Parse(Models.DigitalesRegister.Lesson lesson, ICollection<Teacher> teachers)
	{
		return new()
		{
			ClassId = lesson.ClassId,
			ClassName = lesson.ClassName,
			Date = lesson.Date.ToUniversalTime(),
			Hour = lesson.Hour,
			LinkToPreviousHour = lesson.LinkToPreviousHour,
			RegisterId = lesson.Id,
			Subject = lesson.Subject,
			Teachers = teachers.Where(t => t.Subjects.Contains(lesson.Subject)).ToList(),
			ToHour = lesson.ToHour,
		};
	}

	public static bool operator ==(Lesson? a, Lesson? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.Date == b.Date
			&& a.Hour == b.Hour
			&& a.ToHour == b.ToHour
			&& a.LinkToPreviousHour == b.LinkToPreviousHour
			&& a.ClassId == b.ClassId
			&& a.Subject == b.Subject
			&& a.Teachers.SequenceEqual(b.Teachers, b => b.RegisterID);
	}
	public static bool operator !=(Lesson? a, Lesson? b) => !( a == b );
	public override bool Equals(object? obj) => obj is Lesson other && this == other;
	public override int GetHashCode() => HashCode.Combine(Date, Hour, ToHour, LinkToPreviousHour, ClassId, Subject, Teachers.OrderBy(x => x.RegisterID));
}

public class Subject
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; private set; }
	[Required]
	public required int RegisterId { get; init; }
	[Required]
	public required string Name { get; init; }

	public static implicit operator Subject(Models.DigitalesRegister.Subject subject)
	{
		return new Subject { Name = subject.Name, RegisterId = subject.Id };
	}

	public static bool operator ==(Subject? a, Subject? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.RegisterId == b.RegisterId;
	}
	public static bool operator !=(Subject? a, Subject? b) => !( a == b );
	public override bool Equals(object? obj) => obj is Subject other && this == other;
	public override int GetHashCode() => HashCode.Combine(RegisterId);
}