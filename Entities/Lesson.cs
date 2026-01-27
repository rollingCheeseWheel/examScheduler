using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Util.Extensions;

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
	public required string Name { get; set; }

	[Required]
	public required Subject Subject { get; set; }
	[Required]
	public required ICollection<Teacher> Teachers { get; set; } = [ ];

	[Timestamp]
	public override uint Version { get; set; }

	public bool EqualsModel(Models.DigitalesRegister.Lesson? other) => other is not null
		&& DayOfWeek == other.Date.DayOfWeek
		&& FromHour == Math.Clamp(other.FromHour - 1, 0, 23)
		&& ToHour == Math.Clamp(other.ToHour - 1, 0, 23)
		&& Subject.Name == other.Subject.Name;

	public bool ShallowEqual(Lesson? other) => other is not null
		&& DayOfWeek == other.DayOfWeek
		&& Subject == other.Subject
		&& Teachers.ValueEquals(other.Teachers)
		&& Occurances.ValueEquals(other.Occurances);

	public override bool EqualsCore(Lesson b) =>
		FirstOccurance == b.FirstOccurance &&
		Occurances.ValueEquals(b.Occurances) &&
		FromHour == b.FromHour &&
		Duration == b.Duration &&
		Subject == b.Subject;

	public override int GetHashCode() => HashCode.Combine(FirstOccurance, Occurances.Order(), FromHour, Duration, Subject);

	public override int CompareTo(Lesson? b) => FirstOccurance.CompareTo(b?.FirstOccurance ?? DateTimeOffset.MinValue);
}