using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace examScheduler.Entities;

public class Timetable
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	[Required]
	public required DateTime CreatedAt { get; set; } = DateTime.Now;

	// Navigation Properties
	[Required]
	public ICollection<Week> Data { get; set; } = [ ];
	public int ClassroomId { get; set; }
	[Required]
	public required Classroom Classroom { get; set; }
}

public class Week
{
	public int Id { get; set; }
	[Required]
	public required DateTime Start { get; set; }

	// Navigation Properties
	[Required]
	public ICollection<Day> Days { get; set; } = [ ];
}

public class Day
{
	public int Id { get; set; }
	[Required]
	public required DayOfWeek DayOfWeek { get; set; } // 0 = Sunday, 1 = Monday, ..., 6 = Saturday

	// Navigation Properties
	[Required]
	public ICollection<Lesson> Periods { get; set; } = [ ];
}

public class Lesson
{
	public int Id { get; set; }
	[Required]
	public required DateTime Start { get; set; }
	[Required]
	public required byte StartHour { get; set; } // 0-23
	[Required]
	public required byte DurationInHours { get; set; } // 1-24


	// Navigation Properties
	[Required]
	public required Subject Subject { get; set; }
}

public class Subject
{
	public int Id { get; set; }
	[Required]
	public required string Name { get; set; }

	// Navigation Properties
	[Required]
	public ICollection<Teacher> Teachers { get; set; } = [ ];
}