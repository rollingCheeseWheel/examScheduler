using System.ComponentModel.DataAnnotations;
using Util;
using Util.Validation;

namespace Models.API;

public class AuditLog
{
	[Required]
	public DateTimeOffset Timestamp { get; set; }
	[Required]
	public required string Action { get; set; }
	[Required, DefinedEnum]
	public required AuditLogActor OriginType { get; set; }
	[Required, DefinedGuid]
	public required Guid OriginId { get; set; }
	[Required]
	public required string OriginName { get; set; }
	[DefinedEnum(true)]
	public AuditLogActor? TargetType { get; set; }
	public Guid? TargetId { get; set; }
	public string? TargetName { get; set; }
	public string? Description { get; set; }
}
