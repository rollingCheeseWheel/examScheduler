using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace examScheduler.Entities;

public class Classroom
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

	[Required]
	public string Name { get; set; } = default!;
	public DateTime CreatedAtUTC { get; set; } = DateTime.UtcNow;

	// Navigation Properties
	public Timetable? Timetable { get; set; }
	public ICollection<Student> Students { get; set; } = [];
	public ICollection<Teacher> Teachers { get; set; } = [];
}
