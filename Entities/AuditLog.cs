using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities;

public class AuditLog
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	[Required]
	public DateTime TimestampUTC { get; set; } = DateTime.UtcNow;
	[Required]
	public required Uri RegisterUri { get; set; }
	[Required]
	public required string Action { get; set; }
	[Required]
	public required string PerformedBy { get; set; }
	[Required]
	public required string Details { get; set; }
}
