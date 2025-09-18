using System.ComponentModel.DataAnnotations;

namespace examScheduler.Entities;

public class Classroom
{
	public int Id { get; set; }

	public string Name { get; set; } = default!;
	public DateTime CreatedAtUTC { get; set; } = DateTime.UtcNow;

	// Navigation Properties
	public Timetable Timetable { get; set; } = default!;
	public ICollection<Student> Students { get; set; } = [];
	public ICollection<Teacher> Teachers { get; set; } = [];
}
