using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities;

public class Classroom
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

	[Required]
	[StringLength(255)]
	public required string Name { get; set; }
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

	public override bool Equals(object? obj)
	{
		if (obj is Classroom asClassroom)
		{
			return this == asClassroom;
		}
		else
		{
			return false;
		}
	}

	public static bool operator ==(Classroom a, Classroom b)
	{
		return (
			a.Name == b.Name &&
			a.RegisterId == b.RegisterId
			);
	}

	public static bool operator !=(Classroom a, Classroom b) => !( a == b );
}
