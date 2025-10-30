using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities;

public class Classroom
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

	[Required]
	public required string Name { get; set; }
	[Required]
	public required Uri RegisterUri { get; set; }
	[Required]
	public required int RegisterId { get; set; }
	[Required]
	public required DateTime CreatedAtUTC { get; set; } = DateTime.UtcNow;

	// Navigation Properties
	public Calendar? Calendar { get; set; }
	[Required]
	public required ICollection<Student> Students { get; set; } = [ ];
	[Required]
	public required ICollection<Teacher> Teachers { get; set; } = [ ];
	[Required]
	public required ICollection<Schedule> Schedules { get; set; } = [ ];

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
