using System.ComponentModel.DataAnnotations;

namespace Entities;

public class SwapRequest : EntityBase<SwapRequest>
{
	[Key]
	public override Guid Id { get; set; } = Guid.NewGuid();
	[Required]
	public required Guid ScheduleId { get; set; }
	[Required]
	public required string RequestingStudentName { get; set; }
	[Required]
	public required Guid RequestingStudentId { get; set; }
	[Required]
	public required Guid RequestedSlotId { get; set; }

	[Timestamp]
	public override uint Version { get; set; }

	public override bool EqualsCore(SwapRequest b) => ScheduleId == b.ScheduleId &&
		RequestingStudentId == b.RequestingStudentId &&
		RequestedSlotId == b.RequestedSlotId;

	public override int GetHashCode() => HashCode.Combine(ScheduleId, RequestingStudentId, RequestedSlotId);
}
