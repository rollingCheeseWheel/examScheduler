using System.ComponentModel.DataAnnotations;
using Util;

namespace Entities;

public class AuditLog : EntityBase<AuditLog>
{
    [Key]
    public override Guid Id { get; } = Guid.NewGuid();

    [Required]
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;
    [Required]
    public required Guid Actor { get; init; }
    [Required]
    public required AuditLogActor ActorType { get; init; }
    public string? ActorName { get; init; }
    [Required]
    public required string Action { get; init; }
    public string? Description { get; init; }


    public override bool EqualsCore(AuditLog b) =>
        Id == b.Id &&
        Timestamp == b.Timestamp &&
        Actor == b.Actor &&
        ActorType == b.ActorType &&
        ActorName == b.ActorName &&
        Action == b.Action &&
        Description == b.Description;
    public override int GetHashCode() => HashCode.Combine(Id, Timestamp, Actor, ActorType, ActorName, Action, Description);
    public override int CompareTo(AuditLog? b) => Timestamp.CompareTo(b?.Timestamp ?? DateTimeOffset.MinValue);
}
