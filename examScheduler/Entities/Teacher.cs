namespace examScheduler.Entities;

public class Teacher
{
	public int Id { get; set; }
	public string Name { get; set; } = default!;
	public DateTime CreatedAt { get; set; }

	// Navigation Properties
	public Timetable Timetable { get; set; } = default!;
	public ICollection<Classroom> Classrooms { get; set; } = [];
	public ICollection<Subject> Subjects { get; set; } = [];
}
