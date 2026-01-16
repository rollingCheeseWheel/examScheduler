using System.ComponentModel.DataAnnotations;

namespace Entities;

public class SwapRequest : EntityBase<SwapRequest>
{
	[Required]
	public required Guid ScheduleId { get; set; }
	[Required]
	public required string RequestingStudentName { get; set; }
	[Required]
	public required Guid RequestingStudentId { get; set; }
	[Required]
	public required Guid RequestedStudentId { get; set; }
	[Required]
	public required DateTimeOffset ExpirationDate { get; set; } = DateTimeOffset.UtcNow.AddDays(30);

	public override bool EqualsCore(SwapRequest b) =>
		ScheduleId == b.ScheduleId &&
		RequestingStudentId == b.RequestingStudentId &&
		RequestedStudentId == b.RequestedStudentId &&
		ExpirationDate == b.ExpirationDate;
	public override int GetHashCode() => HashCode.Combine(ScheduleId, RequestingStudentId, RequestedStudentId, ExpirationDate);
	public override int CompareTo(SwapRequest? b) => ExpirationDate.CompareTo(b?.ExpirationDate ?? DateTimeOffset.MinValue);
}
