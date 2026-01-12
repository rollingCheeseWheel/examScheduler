using System.ComponentModel.DataAnnotations;

namespace Entities;

public class SwapRequest
{
	[Key]
	public Guid Id { get; set; } = Guid.NewGuid();
	[Required]
	public required Guid ScheduleId { get; set; }
	[Required]
	public required Guid RequestingStudentId { get; set; }
	[Required]
	public required Guid RequestedStudentId { get; set; }
	[Required]
	public required DateTimeOffset ExpirationDate { get; set; } = DateTimeOffset.UtcNow.AddDays(30);
}
