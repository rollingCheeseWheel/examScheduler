using System.ComponentModel.DataAnnotations;
using Util;

namespace Models.API;

public class AuditLog
{
	[Required]
	public DateTimeOffset Timestamp { get; set; }
	[Required]
	public required Guid Actor { get; set; }
	[Required]
	public required AuditLogActor ActorType { get; set; }
	public string? ActorName { get; set; }
	[Required]
	public required string Action { get; set; }
	public string? Description { get; set; }
}
