namespace ExamScheduler.Entities;

public class Classroom
{
	public int Id { get; set; }
	public string Name { get; set; } = default!;
	public DateTime CreatedAt { get; set; }

	// Foreign Keys
	public Timetable Timetable { get; set; } = default!;
	public ICollection<Student> Students { get; set; } = [];
	public ICollection<Teacher> Teachers { get; set; } = [];
}
