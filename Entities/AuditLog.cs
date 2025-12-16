using System.ComponentModel.DataAnnotations;
using Util;

namespace Entities;

public class AuditLog : IComparable<AuditLog>, IEquatable<AuditLog>
{
	[Key]
	public Guid Id { get; private set; } = Guid.NewGuid();
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

	public static bool operator ==(AuditLog? a, AuditLog? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.Timestamp == b.Timestamp
			&& a.Actor == b.Actor
			&& a.Action == b.Action
			&& a.ActorType == b.ActorType
			&& a.ActorName == b.ActorName
			&& a.Description == b.Description;
	}
	public static bool operator !=(AuditLog? a, AuditLog? b) => !( a == b );
	public override bool Equals(object? obj) => obj is AuditLog other && Equals(other);
	public bool Equals(AuditLog? other) => this == other;
	public override int GetHashCode() => HashCode.Combine(Timestamp, Actor, Action, Description);
	public int CompareTo(AuditLog? other) => Id.CompareTo(other?.Id);
}
