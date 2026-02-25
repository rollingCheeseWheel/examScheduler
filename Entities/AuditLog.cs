using System.ComponentModel.DataAnnotations;
using Util;
using Util.Validation;

namespace Entities;

public class AuditLog : EntityBase<AuditLog>
{
	[Key]
	public override Guid Id { get; set; } = Guid.CreateVersion7();

	[Required]
	public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;
	[Required]
	public required string Action { get; set; }
	[Required, DefinedEnum]
	public required AuditLogActor OriginType { get; set; }
	[Required]
	public required Guid OriginId { get; set; }
	[Required]
	public required string OriginName { get; set; }
	[DefinedEnum(true)]
	public AuditLogTarget? TargetType { get; set; }
	public Guid? TargetId { get; set; }
	public string? TargetName { get; set; }
	public string? Description { get; set; }

	[Timestamp]
	public override uint Version { get; set; }

	public override bool EqualsCore(AuditLog b) => Id == b.Id;

	public override int GetHashCode() => HashCode.Combine(Id);

	public override int CompareTo(AuditLog? b) => Timestamp.CompareTo(b?.Timestamp ?? DateTimeOffset.MinValue);
}

public static class AuditLogAction
{
	public const string EnlistInExamslot = "schedule.enlist";

	public const string CreateSwapRequest = "schedule.swaprequest.create";
	public const string AcceptSwapRequest = "schedule.swaprequest.accept";
	public const string DeleteSwapRequest = "schedule.swaprequest.delete";

	public const string ReportStudents = "schedule.reportstudents";
}