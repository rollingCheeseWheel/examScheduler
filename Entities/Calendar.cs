using System.ComponentModel.DataAnnotations;
using Util.Extensions;

namespace Entities;

public class Calendar : EntityBase<Calendar>
{
	[Key]
	public override Guid Id { get; set; } = Guid.NewGuid();
	[Required]
	public DateTimeOffset LastsUntil { get; set; } = DateTimeOffset.MinValue;
	[Required]
	public ICollection<Lesson> Lessons { get; set; } = [ ];

	[Timestamp]
	public override uint Version { get; set; }

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
			// fill
			for (var hour = 0; hour < longestDayInWeek; hour++)
			{
				lessonMatrix[ day, hour ] = Lessons
					.Where(l =>
						l.DayOfWeek == daysInWeek[ day ] &&
						l.FromHour <= hour &&
						l.ToHour >= hour
					)
					.MaxBy(l => l.Occurances.Count);
			}

			// remove overlaps
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
						Name = lesson.Name,
						Occurances = lesson.Occurances,
						Subject = lesson.Subject,
						Teachers = lesson.Teachers,
					};
					lessonMatrix.TrySet(day, fromHour, replacement);
				}
			}

			// merge
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
						Name = cursor.Name,
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

	public override bool EqualsCore(Calendar other) => Lessons.ValueEquals(other.Lessons);

	public override int GetHashCode() => HashCode.Combine(Lessons.Order());
}
