using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Util.Extensions;
using Util.Validation;

namespace Entities;

public class Lesson : EntityBase<Lesson>
{
	[Key]
	public override Guid Id { get; set; } = Guid.CreateVersion7();
	/// <summary>
	/// Zero-Indexed
	/// </summary>
	[Required, Range(0, 23)]
	public required int FromHour { get; set; }
	/// <inheritdoc path="Lesson.FromHour"/>
	[Required, Range(0, 23), GreaterThanOrEqual<int>(nameof(FromHour))]
	public required int ToHour { get; set; }
	[NotMapped, Range(1, 24)]
	public int Duration => Math.Clamp(ToHour - FromHour + 1, 1, 24);
	[NotMapped]
	public DayOfWeek? DayOfWeek => FirstOccurance?.DayOfWeek;
	[NotMapped]
	public DateTimeOffset? FirstOccurance => Occurances.Count == 0 ? null : Occurances.Order().FirstOrDefault();
	[Required, SameDayOfWeek]
	public required ICollection<DateTimeOffset> Occurances { get; set; } = [ ];
	[Required]
	public required string LessonName { get; set; }

	[Required]
	public required Subject Subject { get; set; }
	[Required]
	public required ICollection<Teacher> Teachers { get; set; } = [ ];

	[Timestamp]
	public override uint Version { get; set; }

	public bool ShallowEqual(Lesson? other) => other is not null
		&& DayOfWeek == other.DayOfWeek
		&& Subject == other.Subject
		&& Teachers.ValueEquals(other.Teachers, t => t.Id)
		&& Occurances.ValueEquals(other.Occurances);

	public override bool EqualsCore(Lesson b) =>
		Occurances.ValueEquals(b.Occurances) &&
		Teachers.ValueEquals(b.Teachers) &&
		FromHour == b.FromHour &&
		Duration == b.Duration &&
		Subject == b.Subject;

	public override int GetHashCode() => HashCode.Combine(FirstOccurance, Occurances.GetValueHashCode(), Teachers.GetValueHashCode(), FromHour, Duration, Subject);

	public override int CompareTo(Lesson? b) => ( FirstOccurance ?? default ).CompareTo(b?.FirstOccurance ?? default);
}