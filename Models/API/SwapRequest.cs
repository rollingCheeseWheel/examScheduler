using System.ComponentModel.DataAnnotations;

namespace Models.API;

public class SwapRequest
{
	[Required]
	public required Guid Id { get; set; }
	[Required]
	public required string RequestingStudentName { get; set; }
	[Required]
	public required Guid RequestingStudentId { get; set; }
	[Required]
	public required Guid RequestedSlotId { get; set; }
}
