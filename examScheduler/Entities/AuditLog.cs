using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace examScheduler.Entities;

public class AuditLog
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	public DateTime TimestampUTC { get; set; } = DateTime.UtcNow;
	[Required]
	public required string Action { get; set; }
	[Required]
	public required string PerformedBy { get; set; }
	[Required]
	public required string Details { get; set; }
}
