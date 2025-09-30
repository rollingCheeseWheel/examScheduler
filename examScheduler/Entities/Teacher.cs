using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace examScheduler.Entities;

public class Teacher
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	[Required]
	public required string Name { get; set; }
	public DateTime CreatedAt { get; set; }

	// Navigation Properties
	public Timetable? Timetable { get; set; }
	[Required]
	public ICollection<Classroom> Classrooms { get; set; } = [];
	[Required]
	public ICollection<Subject> Subjects { get; set; } = [];
}
