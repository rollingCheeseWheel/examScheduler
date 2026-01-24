using System.ComponentModel.DataAnnotations;
using Util;
using Util.Validation;

namespace Entities;

public class AuditLog : EntityBase<AuditLog>
{
	[Key]
	public override Guid Id { get; set; } = Guid.NewGuid();

	[Required]
	public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;
	[Required]
	public required Guid ActorId { get; set; }
	[Required, ValidEnum]
	public required AuditLogActor ActorType { get; set; }
	public string? ActorName { get; set; }
	[Required]
	public required string Action { get; set; }
	public string? Description { get; set; }

	public override bool EqualsCore(AuditLog b) => Id == b.Id &&
		Timestamp == b.Timestamp &&
		ActorId == b.ActorId &&
		ActorType == b.ActorType &&
		ActorName == b.ActorName &&
		Action == b.Action &&
		Description == b.Description;

	public override int GetHashCode() => HashCode.Combine(Id, Timestamp, ActorId, ActorType, ActorName, Action, Description);

	public override int CompareTo(AuditLog? b) => Timestamp.CompareTo(b?.Timestamp ?? DateTimeOffset.MinValue);
}
