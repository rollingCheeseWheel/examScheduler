using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Util.DataStructures;
using Util.Extensions;

namespace Entities;

public class Calendar : EntityBase<Calendar>
{
	[Key]
	public override Guid Id { get; set; } = Guid.CreateVersion7();
	[Required]
	public DateTimeOffset LastsUntil { get; set; }
	[Required]
	public DateTimeOffset LastExtended { get; set; }
	[Required]
	public ICollection<Lesson> Lessons { get; set; } = [ ];

	public Classroom Classroom { get; set; } = null!;

	[NotMapped]
	private Lesson?[ , ]? _fallbackLessonMatrix;
	[NotMapped]
	public Lesson?[ , ] FallbackLessonMatrix
	{
		get
		{
			_fallbackLessonMatrix ??= GetLessonMatrixLessons(Lessons);
			return _fallbackLessonMatrix;
		}
	}

	[NotMapped]
	private IEnumerable<Lesson>? _fallback;
	[NotMapped]
	public IEnumerable<Lesson> FallbackWeek
	{
		get
		{
			_fallback ??= NormalizeToSingleWeek();
			return _fallback;
		}
	}

	[Timestamp]
	public override uint Version { get; set; }

	/// <summary>
	/// Normalizes the collection of lessons to fit within a single week, resolving overlaps (by occurance count) and merging consecutive
	/// lessons where appropriate.
	/// </summary>
	/// <returns>An enumerable collection of lessons adjusted to a single week's schedule, with overlapping lessons managed and
	/// merged as needed.</returns>
	public IEnumerable<Lesson> NormalizeToSingleWeek() => MergeLessonMatrix(FallbackLessonMatrix);

	public IEnumerable<IEnumerable<Lesson>> NormalizeOrDefaultToMostCommonLesson_CreatesNewInstances(DateTimeOffsetRange range = default)
	{
		var groupedByWeek = Lessons
			.Select(l => new Lesson
			{
				Id = l.Id,
				LessonName = l.LessonName,
				Subject = l.Subject,
				FromHour = l.FromHour,
				ToHour = l.ToHour,
				Occurances = [ .. l.Occurances.Where(range.Contains) ],
				Teachers = l.Teachers
			})
			.SelectMany(l => l.Occurances.Select(o => new { Date = o, Lesson = l }))
			.GroupBy(l => l.Date.GetWeek())
			.OrderBy(g => g.Key)
			.Select(g => g
				.Select(x => x.Lesson)
				.ToArray()
			)
			.ToArray();

		var longestDay = Lessons.Max(l => l.FromHour + l.Duration);

		var result = new List<IEnumerable<Lesson>>();
		foreach (var week in groupedByWeek)
		{
			var lessonMatrixForWeek = GetLessonMatrixLessons(week, FallbackLessonMatrix);
			result.Add(MergeLessonMatrix(lessonMatrixForWeek));
		}

		return result;
	}

	/// <summary>
	/// Creates a two dimensional matrix of lessons with resolved overlaps, if the original enumerable of lessons doesnt contain a lesson for the specified hour, it tries to set it to a default from the fallback matrix ([ dayIndex, hourIndex])
	/// </summary>
	/// <param name="lessons"></param>
	/// <returns>[ dayIndex , hourIndex ]</returns>
	private Lesson?[ , ] GetLessonMatrixLessons(IEnumerable<Lesson> lessons, Lesson?[ , ]? fallback = null)
	{
		var longestDay = lessons.Max(l => l.FromHour + l.Duration);
		var lessonMatrix = new Lesson?[ 7, longestDay ];

		for (var dayIndex = 0; dayIndex < 7; dayIndex++)
		{
			// fill
			for (var hour = 0; hour < longestDay; hour++)
			{
				lessonMatrix[ dayIndex, hour ] = Lessons
					.Where(l =>
						l.DayOfWeek == (DayOfWeek)dayIndex &&
						l.FromHour <= hour &&
						l.ToHour >= hour
					)
					.MaxBy(l => l.Occurances.Count)
					?? fallback?.GetOrDefault(dayIndex, hour);
			}

			// remove overlaps
			for (var hour = 0; hour < longestDay; hour++)
			{
				var lesson = lessonMatrix.GetOrDefault(dayIndex, hour);
				if (lesson is null) { continue; }

				for (var fromHour = lesson.FromHour; fromHour < lesson.FromHour + lesson.Duration; fromHour++)
				{
					var valueToOverride = lessonMatrix.GetOrDefault(dayIndex, fromHour);
					if (valueToOverride is not null && valueToOverride.Occurances.Count > lesson.Occurances.Count)
					{
						continue;
					}

					var replacement = new Lesson
					{
						FromHour = fromHour,
						ToHour = lesson.ToHour,
						LessonName = lesson.LessonName,
						Occurances = lesson.Occurances,
						Subject = lesson.Subject,
						Teachers = lesson.Teachers,
					};
					lessonMatrix.TrySet(dayIndex, fromHour, replacement);
				}
			}
		}

		return lessonMatrix;
	}

	/// <summary>
	/// Merges consecutive lessons
	/// </summary>
	/// <param name="lessonMatrix"><see cref="GetLessonMatrixFromWeek(IEnumerable{Lesson})"/>'s lesson matrix ([ dayIndex , hourIndex ])</param>
	/// <returns>List containing the merged lessons</returns>
	private static IEnumerable<Lesson> MergeLessonMatrix(Lesson?[ , ] lessonMatrix)
	{
		var result = new List<Lesson>();

		Lesson? cursor = null;
		for (var dayIndex = 0; dayIndex < lessonMatrix.GetLength(0); dayIndex++)
		{
			for (var hourIndex = 0; hourIndex < lessonMatrix.GetLength(1); hourIndex++)
			{
				var lesson = lessonMatrix.GetOrDefault(dayIndex, hourIndex);
				if (lesson is null) { continue; }
				if (cursor is null || !cursor.ShallowEqual(lesson))
				{
					cursor = lesson;
					result.Add(lesson);
				}
				else
				{
					cursor = new()
					{
						FromHour = cursor.FromHour,
						ToHour = lesson.ToHour,
						LessonName = cursor.LessonName,
						Occurances = cursor.Occurances,
						Subject = cursor.Subject,
						Teachers = cursor.Teachers,
					};
					result[ ^1 ] = cursor;
				}
			}
		}

		return result;
	}

	public override bool EqualsCore(Calendar other) => Lessons.ValueEquals(other.Lessons);

	public override int GetHashCode() => Lessons.GetValueHashCode();
}
