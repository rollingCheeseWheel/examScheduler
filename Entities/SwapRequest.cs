using System.ComponentModel.DataAnnotations;

namespace Entities;

public class SwapRequest
{
	[Key]
	public Guid Id { get; set; } = Guid.NewGuid();
	[Required]
	public required School School { get; set; }
	[Required]
	public required Schedule Schedule { get; set; }
	public Guid ScheduleId { get; set; }
	[Required]
	public required Guid FirstStudent { get; set; }
	[Required]
	public required Guid SecondStudent { get; set; }
}
