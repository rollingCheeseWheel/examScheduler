using System.ComponentModel.DataAnnotations;

namespace Entities;

public class ScheduleGeneratorSlot : EntityBase<ScheduleGeneratorSlot>
{
	[Key]
	public override Guid Id { get; set; } = Guid.NewGuid();
	[Required, Range(0, int.MaxValue)]
	public required int Offset { get; set; }
	[Required, Range(0, int.MaxValue)]
	public required int MaxParticipants { get; set; }
	[Required, Range(0, int.MaxValue)]
	public required int MinParticipants { get; set; }

	public override bool EqualsCore(ScheduleGeneratorSlot b)
	{
		return Offset == b.Offset &&
		MaxParticipants == b.MaxParticipants &&
		MinParticipants == b.MinParticipants;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Offset, MaxParticipants, MinParticipants);
	}

	public override int CompareTo(ScheduleGeneratorSlot? other)
	{
		if (other is null) { return 1; }
		var res = Offset.CompareTo(other.Offset);
		if (res != 0) { return res; }
		res = MinParticipants.CompareTo(other.MinParticipants);
		if (res != 0) { return res; }
		res = MaxParticipants.CompareTo(other.MaxParticipants);
		return res != 0 ? res : Id.CompareTo(other.Id);
	}
}