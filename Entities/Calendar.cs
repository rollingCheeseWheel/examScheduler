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
					LessonId = iterLesson.LessonId,
					LessonName = iterLesson.LessonName,
					FromHour = Math.Clamp(iterLesson.FromHour - 1, 0, 23),
					ToHour = Math.Clamp(iterLesson.ToHour - 1, 0, 23),
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
		var result = new List<Lesson>();

		var daysInWeek = Enum.GetValues<DayOfWeek>();
		var longestDayInWeek = Lessons
			.GroupBy(l => l.DayOfWeek)
			.Max(g => g.Select(l => l.FromHour + l.Duration).Max());
		var lessonMatrix = new Lesson?[ daysInWeek.Length, longestDayInWeek ];

		for (var day = 0; day < daysInWeek.Length; day++)
		{
			for (var hour = 0; hour < longestDayInWeek; hour++)
			{
				lessonMatrix[ day, hour ] = Lessons
					.Where(l
						=> l.DayOfWeek == daysInWeek[ day ]
						&& l.FromHour <= hour
						&& l.ToHour >= hour
					)
					.MaxBy(l => l.Occurances.Count);
			}

			for (var hour = 0; hour < longestDayInWeek; hour++)
			{
				var lesson = lessonMatrix.GetOrDefault(day, hour);
				if (lesson is null) { continue; }

				for (var fromHour = lesson.FromHour; fromHour < lesson.FromHour + lesson.Duration; fromHour++)
				{
					var valueToOverride = lessonMatrix.GetOrDefault(day, fromHour);
					if (valueToOverride is not null && valueToOverride.Occurances.Count > lesson.Occurances.Count)
					{
						continue;
					}

					var replacement = new Lesson
					{
						FromHour = fromHour,
						ToHour = lesson.ToHour,

						LessonId = lesson.LessonId,
						LessonName = lesson.LessonName,
						Occurances = lesson.Occurances,
						Subject = lesson.Subject,
						Teachers = lesson.Teachers,
					};
					lessonMatrix.TrySet(day, fromHour, replacement);
				}
			}

			Lesson? cursor = null;
			var tempResult = new List<Lesson>();
			for (var hour = 0; hour < longestDayInWeek; hour++)
			{
				var lesson = lessonMatrix.GetOrDefault(day, hour);
				if (lesson is null) { continue; }
				if (cursor is null || !cursor.ShallowEqual(lesson))
				{
					cursor = lesson;
					tempResult.Add(lesson);
				}
				else
				{
					cursor = new()
					{
						FromHour = cursor.FromHour,
						ToHour = lesson.ToHour,


						LessonId = cursor.LessonId,
						LessonName = cursor.LessonName,
						Occurances = cursor.Occurances,
						Subject = cursor.Subject,
						Teachers = cursor.Teachers,
					};
					tempResult[ ^1 ] = cursor;
				}
			}
			result.AddRange(tempResult);
		}

		return result;
	}

	public static bool operator ==(Calendar? a, Calendar? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.Classroom == b.Classroom
			&& a.Lessons.ValueEquals(b.Lessons, x => x.FirstOccurance);
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
	/// <summary>
	/// Zero-Indexed
	/// </summary>
	[Required, Range(0, 23)]
	public required int FromHour { get; set; }
	[Required, Range(0, 23)]
	public required int ToHour { get; set; }
	[NotMapped, Range(1, 24)]
	public int Duration => Math.Clamp(ToHour - FromHour + 1, 1, 24);
	[Required]
	public required int LessonId { get; set; }
	[Required]
	public required string LessonName { get; set; }

	public required ICollection<Teacher> Teachers { get; set; } = [ ];
	[Required]
	public required Subject Subject { get; set; }

	public static bool operator ==(Lesson? a, Lesson? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.FirstOccurance == b.FirstOccurance
			&& a.Occurances.ValueEquals(b.Occurances, x => x)
			&& a.FromHour == b.FromHour
			&& a.Duration == b.Duration
			&& a.LessonId == b.LessonId
			&& a.Subject == b.Subject
			&& a.Teachers.ValueEquals(b.Teachers, x => x.RegisterID);
	}
	public static bool operator !=(Lesson? a, Lesson? b) => !( a == b );
	public override bool Equals(object? obj) => obj is Lesson other && Equals(other);
	public bool Equals(Lesson? other) => this == other;
	public bool EqualsModel(Models.DigitalesRegister.Lesson? other)
	{
		if (other is null) { return false; }
		return DayOfWeek == other.Date.DayOfWeek
			&& FromHour == Math.Clamp(other.FromHour - 1, 0, 23)
			&& ToHour == Math.Clamp(other.ToHour - 1, 0, 23)
			&& LessonId == other.LessonId
			&& Subject.EqualsModel(other.Subject);
	}
	public override int GetHashCode() => HashCode.Combine(FirstOccurance, Occurances.Order(), FromHour, Duration, LessonId, Subject, Teachers.OrderBy(t => t.RegisterID));
	public int CompareTo(Lesson? other)
	{
		if (other is null) { return 1; }
		var res = FirstOccurance.CompareTo(other.FirstOccurance);
		if (res != 0) { return res; }
		res = FromHour.CompareTo(other.FromHour);
		if (res != 0) { return res; }
		res = Duration.CompareTo(other.Duration);
		if (res != 0) { return res; }
		return Id.CompareTo(other.Id);
	}

	public bool ShallowEqual(Lesson? other)
	{
		if (other is null) { return false; }
		return DayOfWeek == other.DayOfWeek
			&& LessonId == other.LessonId
			&& Subject == other.Subject
			&& Teachers.ValueEquals(other.Teachers, x => x.RegisterID)
			&& Occurances.ValueEquals(other.Occurances, x => x);
	}
}