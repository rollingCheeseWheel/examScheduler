using System.ComponentModel.DataAnnotations;

namespace Entities;

public class AuditLog
{
	[Key]
	public Guid Id { get; private set; }
	[Required]
	public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;
	[Required]
	public required string Action { get; init; }
	[Required]
	public required string Actor { get; init; }
	[Required]
	public required string Description { get; init; }
	[Required]
	public required Classroom Classroom { get; init; }
}
