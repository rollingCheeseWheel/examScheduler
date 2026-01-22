using System.ComponentModel.DataAnnotations;

namespace Models.API;

public class SwapRequest
{
	[Required]
	public required Guid Id { get; set; }
	[Required]
	public required Guid ScheduleId { get; set; }
	[Required]
	public required string RequestedStudentName { get; set; }
	[Required]
	public required string RequestingStudentName { get; set; }
	[Required]
	public required Guid RequestedStudentId { get; set; }
	[Required]
	public required Guid RequestingStudentId { get; set; }
	[Required]
	public required DateTimeOffset ExpirationDate { get; set; }
}
