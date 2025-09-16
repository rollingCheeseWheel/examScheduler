namespace ExamScheduler.Entities;

public class Timetable
{
	public int Id { get; set; }
	public DateTime CreatedAt { get; set; }

	// Foreign Keys
	public ICollection<Week> Data { get; set; } = [];
	public Classroom Classrooms { get; set; } = default!;
}

public class Week
{
	public int Id { get; set; }
	public DateTime Start { get; set; }

	// Foreign Keys
	public ICollection<Day> Days { get; set; } = [];
}

public class Day
{
	public int Id { get; set; }
	public DayOfWeek DayOfWeek { get; set; } // 0 = Sunday, 1 = Monday, ..., 6 = Saturday

	// Foreign Keys
	public ICollection<Lesson> Periods { get; set; } = [];
}

public class Lesson
{
	public int Id { get; set; }
	public DateTime Start { get; set; }
	public byte StartHour { get; set; } // 0-23
	public byte DurationInHours { get; set; } // 1-24


	// Foreign Keys
	public Subject Subject { get; set; } = default!;
}

public class Subject
{
	public int Id { get; set; }
	public string Name { get; set; } = default!;

	// Foreign Keys
	public ICollection<Teacher> Teachers { get; set; } = [];
}