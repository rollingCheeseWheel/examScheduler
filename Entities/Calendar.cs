using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using Util;

namespace Entities;

public class Calendar() : IComparable<Calendar>
{
	[Key]
	public Guid Id { get; set; }

	[Required]
	public DateTimeOffset LastsUntil { get; set; } = DateTimeOffset.UtcNow;

	[Required]
	public ICollection<Lesson> Lessons { get; set; } = [ ];
	public Classroom? Classroom { get; set; }

	public void Extend(IEnumerable<Models.DigitalesRegister.CalendarDay> days)
	{
		foreach (var iterLesson in days.SelectMany(d => d.Lessons))
		{
			var existingLesson = Lessons.Where(l => l.DayOfWeek == iterLesson.Date.DayOfWeek).FirstOrDefault();
			if (existingLesson is null)
			{
				existingLesson = new()
				{
					ClassId = iterLesson.ClassId,
					ClassName = iterLesson.ClassName,
					Hour = iterLesson.Hour,
					ToHour = iterLesson.ToHour,
					Occurances = [ iterLesson.Date ],
					Subject = iterLesson.Subject,
					//Teachers = iterLesson.Teachers, // TODO add teacher and subject parser
					Teachers = [ ]
				};
				Lessons.Add(existingLesson);
			}
			else
			{

			}
		}
	}

	public IEnumerable<Lesson> Normalize()
	{
		throw new NotImplementedException();
	}

	public static bool operator ==(Calendar? a, Calendar? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.Classroom == b.Classroom
			&& a.Lessons.SequenceEqual(b.Lessons, x => x.FirstOccurance);
	}
	public static bool operator !=(Calendar? a, Calendar? b) => !( a == b );
	public override bool Equals(object? obj) => obj is Calendar other && this == other;
	public override int GetHashCode() => HashCode.Combine(Classroom, Lessons.OrderBy(b => b.FirstOccurance));
	public int CompareTo(Calendar? other) => Id.CompareTo(other?.Id);
}

public class Lesson : IComparable<Lesson>
{
	[Key]
	public Guid Id { get; set; }

	[NotMapped]
	public DayOfWeek DayOfWeek => FirstOccurance.DayOfWeek;

	[NotMapped]
	public DateTimeOffset FirstOccurance => Occurances.Order().FirstOrDefault();
	[Required]
	public required ICollection<DateTimeOffset> Occurances { get; set; } = [ ];
	[Required]
	public required int Hour { get; set; }
	[Required]
	public required int ToHour { get; set; }
	[Required]
	public required int ClassId { get; set; }
	[Required]
	public required string ClassName { get; set; }

	public required ICollection<Teacher> Teachers { get; set; } = [ ];
	[Required]
	public required Subject Subject { get; set; }

	[NotMapped]
	public int Duration => ToHour - Hour + 1;

	public static Lesson Parse(Models.DigitalesRegister.Lesson lesson, ICollection<Teacher> teachers)
	{
		return new()
		{
			ClassId = lesson.ClassId,
			ClassName = lesson.ClassName,
			Occurances = [ lesson.Date.ToUniversalTime() ],
			Hour = lesson.Hour,
			Subject = lesson.Subject,
			Teachers = teachers.Where(t => t.Subjects.Contains(lesson.Subject)).ToList(),
			ToHour = lesson.ToHour,
		};
	}

	public static bool operator ==(Lesson? a, Lesson? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.FirstOccurance == b.FirstOccurance
			&& a.Occurances.SequenceEqual(b.Occurances, x => x)
			&& a.Hour == b.Hour
			&& a.ToHour == b.ToHour
			&& a.ClassId == b.ClassId
			&& a.Subject == b.Subject
			&& a.Teachers.SequenceEqual(b.Teachers, b => b.RegisterID);
	}
	public static bool operator !=(Lesson? a, Lesson? b) => !( a == b );
	public override bool Equals(object? obj) => obj is Lesson other && this == other;
	public override int GetHashCode() => HashCode.Combine(FirstOccurance, Occurances.Order(), Hour, ToHour, ClassId, Subject, Teachers.OrderBy(t => t.RegisterID));
	public int CompareTo(Lesson? other)
	{
		if (other is null) { return 1; }
		var res = FirstOccurance.CompareTo(other.FirstOccurance);
		if (res != 0) { return res; }
		res = Hour.CompareTo(other.Hour);
		if (res != 0) { return res; }
		res = ToHour.CompareTo(other.ToHour);
		if (res != 0) { return res; }
		return Id.CompareTo(other.Id);
	}
}