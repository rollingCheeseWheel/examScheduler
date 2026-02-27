using System.ComponentModel.DataAnnotations;
using Util.DataStructures;
using Util.Extensions;
using Util.Validation;

namespace Entities;

public class ScheduleGenerator : EntityBase<ScheduleGenerator>
{
	[Key]
	public override Guid Id { get; set; } = Guid.CreateVersion7();

	[Required, DistinctBy<DayOfWeek>(nameof(ScheduleGeneratorSlot.DayOfWeek))]
	public required ICollection<ScheduleGeneratorSlot> GeneratorSlots { get; set; } = [ ];
	[Required, Distinct<DateOnly>, MaxLength(20)]
	public required ICollection<DateOnly> BlacklistedDays { get; set; } = [ ];


	[Timestamp]
	public override uint Version { get; set; }

	public LoopingEnumerable<ScheduleGeneratorSlot> GetLoopingEnumerable(int maxIterations = -1) => new([ .. GeneratorSlots ], maxIterations);

	public override bool EqualsCore(ScheduleGenerator b) => GeneratorSlots.ValueEquals(b.GeneratorSlots) && BlacklistedDays.ValueEquals(b.BlacklistedDays);
	public override int GetHashCode() => HashCode.Combine(GeneratorSlots.GetValueHashCode(), BlacklistedDays.GetValueHashCode());
}

public class ScheduleGeneratorSlot : EntityBase<ScheduleGeneratorSlot>
{
	[Key]
	public override Guid Id { get; set; }

	[Required, DefinedEnum]
	public required DayOfWeek DayOfWeek { get; set; }
	[Required, MinValue(0)]
	public required int MinParticipants { get; set; }
	[Required, GreaterThan<int>(nameof(MinParticipants))]
	public required int MaxParticipants { get; set; }


	[Timestamp]
	public override uint Version { get; set; }

	public override bool EqualsCore(ScheduleGeneratorSlot b) =>
		DayOfWeek == b.DayOfWeek &&
		MinParticipants == b.MinParticipants &&
		MaxParticipants == b.MaxParticipants;
	public override int GetHashCode() => HashCode.Combine(DayOfWeek, MinParticipants, MaxParticipants);
	public override int CompareTo(ScheduleGeneratorSlot? b) => DayOfWeek.CompareTo(b?.DayOfWeek ?? (DayOfWeek)(-1));
}