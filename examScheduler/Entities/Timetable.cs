using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace examScheduler.Entities;

public class Timetable
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	public DateTime CreatedAt { get; set; }

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
	public DateTime Start { get; set; }

	// Navigation Properties
	public ICollection<Day> Days { get; set; } = [ ];
}

public class Day
{
	public int Id { get; set; }
	public DayOfWeek DayOfWeek { get; set; } // 0 = Sunday, 1 = Monday, ..., 6 = Saturday

	// Navigation Properties
	public ICollection<Lesson> Periods { get; set; } = [ ];
}

public class Lesson
{
	public int Id { get; set; }
	public DateTime Start { get; set; }
	public byte StartHour { get; set; } // 0-23
	public byte DurationInHours { get; set; } // 1-24


	// Navigation Properties
	public Subject Subject { get; set; } = default!;
}

public class Subject
{
	public int Id { get; set; }
	public string Name { get; set; } = default!;

	// Navigation Properties
	public ICollection<Teacher> Teachers { get; set; } = [ ];
}