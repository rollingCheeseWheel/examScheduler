using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities;

public class AuditLog
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; }
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
