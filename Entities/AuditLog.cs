using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities;

public class AuditLog
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; private set; }
	[Required]
	public DateTime TimestampUTC { get; } = DateTime.UtcNow;
	[Required]
	public required string Action { get; init; }
	[Required]
	public required string PerformedBy { get; init; }
	[Required]
	public required string Details { get; init; }
	[Required]
	public required Classroom Classroom { get; init; }
}
