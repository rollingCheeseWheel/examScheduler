using System.ComponentModel.DataAnnotations;

namespace Entities;

public class Classroom() : IComparable<Classroom>
{
	[Key]
	public Guid Id { get; private set; } = Guid.NewGuid();
	[Required]
	public required string Name { get; init; }
	[Required]
	public required School School { get; init; }
	public Guid SchoolId { get; }
	[Required]
	public required long RegisterId { get; init; }
	[Required]
	public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;

	// Navigation Properties
	public Calendar? Calendar { get; set; }
	[Required]
	public ICollection<StudentProfile> Students { get; set; } = [ ];
	[Required]
	public ICollection<Teacher> Teachers { get; set; } = [ ];
	[Required]
	public ICollection<Schedule> Schedules { get; set; } = [ ];

	public static bool operator ==(Classroom? a, Classroom? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.RegisterId == b.RegisterId
			&& a.School == b.School;
	}
	public static bool operator !=(Classroom? a, Classroom? b) => !( a == b );
	public override bool Equals(object? obj) => obj is Classroom other && this == other;
	public override int GetHashCode() => HashCode.Combine(RegisterId, School);
	public int CompareTo(Classroom? other) => Id.CompareTo(other?.Id);
}
