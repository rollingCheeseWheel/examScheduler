namespace examScheduler.Entities;

public class AuditLog
{
	public int Id { get; set; }
	public DateTime TimestampUTC { get; set; } = DateTime.UtcNow;
	public string Action { get; set; } = default!;
	public string PerformedBy { get; set; } = default!;
	public string Details { get; set; } = default!;
}
