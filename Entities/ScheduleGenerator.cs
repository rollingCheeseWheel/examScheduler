using System.ComponentModel.DataAnnotations;
using Util.Extensions;
using Util.Validation;

namespace Entities;

public class ScheduleGenerator : EntityBase<ScheduleGenerator>
{
	[Key, DefinedGuid]
	public override Guid Id { get; set; } = Guid.CreateVersion7();

	[Required]
	public required Guid ScheduleId { get; set; }

	[Required, ICollectionDistinctBy<ScheduleGeneratorSlot>(nameof(ScheduleGeneratorSlot.DayOfWeek))]
	public required ICollection<ScheduleGeneratorSlot> GeneratorSlots { get; set; } = [ ];
	[Required, ICollectionDistinct<DateTimeOffset>, MaxLength(20)]
	public required ICollection<DateTimeOffset> BlacklistedDays { get; set; } = [ ];


	[Timestamp]
	public override uint Version { get; set; }

	public override bool EqualsCore(ScheduleGenerator b) => GeneratorSlots.ValueEquals(b.GeneratorSlots) && BlacklistedDays.ValueEquals(b.BlacklistedDays);
	public override int GetHashCode() => HashCode.Combine(GeneratorSlots.GetValueHashCode(), BlacklistedDays.GetValueHashCode());
}

public class ScheduleGeneratorSlot : EntityBase<ScheduleGeneratorSlot>
{
	[Key, DefinedGuid]
	public override Guid Id { get; set; } = Guid.CreateVersion7();

	[Required, DefinedEnum]
	public required DayOfWeek DayOfWeek { get; set; }
	[Required, MinValue(1)]
	public required int MaxParticipants { get; set; }


	[Timestamp]
	public override uint Version { get; set; }

	public override bool EqualsCore(ScheduleGeneratorSlot b) =>
		DayOfWeek == b.DayOfWeek &&
		MaxParticipants == b.MaxParticipants;
	public override int GetHashCode() => HashCode.Combine(DayOfWeek, MaxParticipants);
	public override int CompareTo(ScheduleGeneratorSlot? b) => DayOfWeek.CompareTo(b?.DayOfWeek ?? (DayOfWeek)( -1 ));
}