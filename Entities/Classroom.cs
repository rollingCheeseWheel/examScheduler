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
	public required DateTime CreatedAtUTC { get; set; } = DateTime.UtcNow;

	// Navigation Properties
	public Timetable? Timetable { get; set; }
	[Required]
	public required ICollection<Student> Students { get; set; } = [ ];
	[Required]
	public required ICollection<Teacher> Teachers { get; set; } = [ ];
}
