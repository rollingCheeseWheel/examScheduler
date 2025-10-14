using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities;

public class AuditLog
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	public DateTime TimestampUTC { get; set; } = DateTime.UtcNow;
	[Required]
	[StringLength(255)]
	public required string Action { get; set; }
	[Required]
	[StringLength(255)]
	public required string PerformedBy { get; set; }
	[Required]
	[StringLength(1024)]
	public required string Details { get; set; }
}
