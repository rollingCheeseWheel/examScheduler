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
	public required DateTime CreatedAtUTC { get; set; } = DateTime.UtcNow;

	// Navigation Properties
	public Calendar? Timetable { get; set; }
	[Required]
	public required ICollection<Student> Students { get; set; } = [ ];
	[Required]
	public required ICollection<Teacher> Teachers { get; set; } = [ ];
}
