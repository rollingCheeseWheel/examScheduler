using System.ComponentModel.DataAnnotations;
using Util.Validation;

namespace Entities;

public class ScheduleGeneratorSlot : EntityBase<ScheduleGeneratorSlot>
{
	[Key]
	public override Guid Id { get; set; } = Guid.NewGuid();
	[Required, PositiveTimeSpan]
	public required TimeSpan Offset { get; set; }
	[Required, Range(0, int.MaxValue)]
	public required int MaxParticipants { get; set; }
	[Required, Range(0, int.MaxValue)]
	public required int MinParticipants { get; set; }

	[Timestamp]
	public override uint Version { get; set; }

	public override bool EqualsCore(ScheduleGeneratorSlot b) => Offset == b.Offset &&
		MaxParticipants == b.MaxParticipants &&
		MinParticipants == b.MinParticipants;

	public override int GetHashCode() => HashCode.Combine(Offset, MaxParticipants, MinParticipants);

	public override int CompareTo(ScheduleGeneratorSlot? other) => Offset.CompareTo(other?.Offset ?? TimeSpan.MinValue);
}