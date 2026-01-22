using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Util;

namespace Entities;

public class Lesson : EntityBase<Lesson>
{
	[Key]
	public override Guid Id { get; set; } = Guid.NewGuid();
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
		return other is not null && DayOfWeek == other.Date.DayOfWeek
			&& FromHour == Math.Clamp(other.FromHour - 1, 0, 23)
			&& ToHour == Math.Clamp(other.ToHour - 1, 0, 23)
			&& LessonId == other.LessonId
			&& Subject.EqualsModel(other.Subject);
	}

	public bool ShallowEqual(Lesson? other)
	{
		return other is not null && DayOfWeek == other.DayOfWeek
			&& LessonId == other.LessonId
			&& Subject == other.Subject
			&& Teachers.ValueEquals(other.Teachers, x => x.RegisterID)
			&& Occurances.ValueEquals(other.Occurances, x => x);
	}

	public override bool EqualsCore(Lesson b)
	{
		return FirstOccurance == b.FirstOccurance &&
		Occurances.ValueEquals(b.Occurances) &&
		FromHour == b.FromHour &&
		Duration == b.Duration &&
		Subject == b.Subject;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(FirstOccurance, Occurances.Order(), FromHour, Duration, Subject);
	}

	public override int CompareTo(Lesson? b)
	{
		return FirstOccurance.CompareTo(b?.FirstOccurance ?? DateTimeOffset.MinValue);
	}
}