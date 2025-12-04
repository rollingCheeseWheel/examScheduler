using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Util;

namespace Entities;

public class Calendar : IComparable<Calendar>, IEquatable<Calendar>
{
	[Key]
	public Guid Id { get; private set; } = Guid.NewGuid();

	[Required]
	public DateTimeOffset LastsUntil { get; set; } = DateTimeOffset.UtcNow;

	[Required]
	public ICollection<Lesson> Lessons { get; set; } = [ ];
	[Required]
	public required Classroom Classroom { get; set; }
	public Guid ClassroomId { get; private set; }

	public void Extend(IEnumerable<Models.DigitalesRegister.Lesson> lessons, School school, out IEnumerable<Teacher> createdTeachers, out IEnumerable<Subject> createdSubjects)
	{
		var existingTeacherSubjects = Lessons
			.GroupBy(l => l.Subject)
			.Select(g => (
				Subject: g.Key,
				Teachers: g.SelectMany(l => l.Teachers).Distinct(),
				Lessons: (IEnumerable<Models.DigitalesRegister.Lesson>)[ ]
			));

		var registerTeacherSubjects = lessons
			.GroupBy(l => l.Subject)
			.Select(g => (
				Subject: new Subject()
				{
					Name = g.Key.Name,
					RegisterId = g.Key.Id
				},
				Teachers: g.SelectMany(l => l.Teachers).Distinct()
				.Select(t => new Teacher()
				{
					FirstName = t.FirstName,
					LastName = t.LastName,
					RegisterID = t.Id,
					School = school,
				}),
				Lessons: g.Select(l => l)
			));

		var additionalTeacherSubjects = registerTeacherSubjects.Except(existingTeacherSubjects, g => g.Subject);

		createdSubjects = additionalTeacherSubjects.Select(g => g.Subject);
		createdTeachers = additionalTeacherSubjects.SelectMany(g => g.Teachers);

		var combinedTeacherSubjects = existingTeacherSubjects
			.Concat(additionalTeacherSubjects)
			.GroupBy(g => g.Subject)
			.Select(g => (
				Subject: g.Key,
				Teachers: g.SelectMany(x => x.Teachers),
				Lessons: g.SelectMany(x => x.Lessons)
			));

		foreach (var iterLesson in lessons)
		{
			var matchingTeacherSubjects = combinedTeacherSubjects.FirstOrDefault(g => g.Lessons.Contains(iterLesson));
			var existingLesson = Lessons
				.Where(l => l.EqualsModel(iterLesson))
				.FirstOrDefault();
			if (existingLesson is null)
			{

				existingLesson = new()
				{
					ClassId = iterLesson.ClassId,
					ClassName = iterLesson.ClassName,
					Hour = iterLesson.Hour,
					ToHour = iterLesson.ToHour,
					Occurances = [ iterLesson.Date ],
					Subject = matchingTeacherSubjects.Subject,
					Teachers = matchingTeacherSubjects.Teachers.ToList(),
				};
				Lessons.Add(existingLesson);
			}
			else
			{
				existingLesson.Occurances.Add(iterLesson.Date);
				existingLesson.Teachers.Clear();
				foreach (var teacher in matchingTeacherSubjects.Teachers)
				{
					existingLesson.Teachers.Add(teacher);
				}
			}
		}
	}

	public IEnumerable<Lesson> Normalize()
	{
		var daysInWeek = Enum.GetValues<DayOfWeek>();
		var longestDayInWeek = Lessons.Max(l => l.ToHour);
		var lessonMatrix = new Lesson?[ daysInWeek.Length, longestDayInWeek ];

		for (var day = 0; day < daysInWeek.Length; day++)
		{
			for (var hour = 0; hour < longestDayInWeek; hour++)
			{
				lessonMatrix[ day, hour ] = Lessons
					.Where(l
						=> l.DayOfWeek == daysInWeek[ day ]
						&& l.Hour <= hour
						&& l.ToHour >= hour
					)
					.MaxBy(l => l.Occurances.Count);
			}

			// TODO need to filter through the list and split every lesson that spans more than one hour 
		}

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
	public override bool Equals(object? obj) => obj is Calendar other && Equals(other);
	public bool Equals(Calendar? other) => this == other;
	public override int GetHashCode() => HashCode.Combine(Classroom, Lessons.OrderBy(b => b.FirstOccurance));
	public int CompareTo(Calendar? other) => Id.CompareTo(other?.Id);
}

public class Lesson : IComparable<Lesson>, IEquatable<Lesson>
{
	[Key]
	public Guid Id { get; private set; } = Guid.NewGuid();

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
	public override bool Equals(object? obj) => obj is Lesson other && Equals(other);
	public bool Equals(Lesson? other) => this == other;
	public bool EqualsModel(Models.DigitalesRegister.Lesson? other)
	{
		if (other is null) { return false; }
		return DayOfWeek == other.Date.DayOfWeek
			&& Hour == other.Hour
			&& ToHour == other.ToHour
			&& ClassId == other.ClassId
			&& Subject.EqualsModel(other.Subject);
	}
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