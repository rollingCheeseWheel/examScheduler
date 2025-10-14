using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities;

public class Timetable
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	[Required]
	public required DateTime CreatedAt { get; set; } = DateTime.Now;

	// Navigation Properties
	[Required]
	public IEnumerable<CalendarWeek> Data { get; set; } = [ ];
	[Required]
	public int ClassroomId { get; set; }
	[Required]
	public required Classroom Classroom { get; set; }
}