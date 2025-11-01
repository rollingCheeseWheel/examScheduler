using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities;

public class Classroom
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; private set; }

	[Required]
	public required string Name { get; init; }
	[Required]
	public required Uri RegisterUri { get; init; }
	[Required]
	public required int RegisterId { get; init; }
	[Required]
	public DateTime CreatedAtUTC { get; } = DateTime.UtcNow;

	// Navigation Properties
	public Calendar? Calendar { get; set; }
	[Required]
	public ICollection<Student> Students { get; private set; } = [ ];
	[Required]
	public ICollection<Teacher> Teachers { get; private set; } = [ ];
	[Required]
	public ICollection<Schedule> Schedules { get; private set; } = [ ];
	[Required]
	public ICollection<AuditLog> AuditLogs { get; private set; } = [ ];

	public static bool operator ==(Classroom? a, Classroom? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.RegisterId == b.RegisterId
			&& a.RegisterUri == b.RegisterUri;
	}

	public static bool operator !=(Classroom? a, Classroom? b) => !( a == b );
	public override bool Equals(object? obj) => obj is Classroom other && this == other;
	public override int GetHashCode() => HashCode.Combine(RegisterId, RegisterUri);
}
