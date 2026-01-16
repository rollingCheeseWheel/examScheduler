using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Util;

namespace Entities;

public class Calendar : EntityBase<Calendar>
{

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

	public override bool EqualsCore(Calendar other) =>
		Classroom == other.Classroom &&
		Lessons.ValueEquals(other.Lessons);
	public override int GetHashCode() => HashCode.Combine(Classroom, Lessons.Order());
    public override int CompareTo(Calendar? b) => Classroom.CompareTo(b?.Classroom);
}

public class Lesson : EntityBase<Lesson>
{
	/// <summary>
	/// Zero-Indexed
	/// </summary>
	[Required, Range(0, 23)]
	public required int FromHour { get; set; }
	/// <inheritdoc path="Lesson.FromHour"/>
	[Required, Range(0, 23)]
	public required int ToHour { get; set; }
	[NotMapped, Range(1, 24)]
	public int Duration => Math.Clamp(ToHour - FromHour + 1, 1, 24);
	[NotMapped]
	public DayOfWeek DayOfWeek => FirstOccurance.DayOfWeek;
	[NotMapped]
	public DateTimeOffset FirstOccurance => Occurances.Order().FirstOrDefault();
	[Required]
	public required ICollection<DateTimeOffset> Occurances { get; set; } = [ ];
	[Required]
	public required int LessonId { get; set; }
	[Required]
	public required string LessonName { get; set; }

	[Required]
	public required Subject Subject { get; set; }
	public required ICollection<Teacher> Teachers { get; set; } = [ ];

	public bool EqualsModel(Models.DigitalesRegister.Lesson? other)
	{
		if (other is null) { return false; }
		return DayOfWeek == other.Date.DayOfWeek
			&& FromHour == Math.Clamp(other.FromHour - 1, 0, 23)
			&& ToHour == Math.Clamp(other.ToHour - 1, 0, 23)
			&& LessonId == other.LessonId
			&& Subject.EqualsModel(other.Subject);
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

	public override bool EqualsCore(Lesson b) =>
		FirstOccurance == b.FirstOccurance &&
		Occurances.ValueEquals(b.Occurances) &&
		FromHour == b.FromHour &&
		Duration == b.Duration &&
		Subject == b.Subject;
	public override int GetHashCode() => HashCode.Combine(FirstOccurance, Occurances.Order(), FromHour, Duration, Subject);
	public override int CompareTo(Lesson? b) => FirstOccurance.CompareTo(b?.FirstOccurance ?? DateTimeOffset.MinValue);
}