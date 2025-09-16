namespace ExamScheduler.Entities;

public class Schedule
{
	public int Id { get; set; }
	public AutoLockIn AutoLockIn { get; set; }
	public DateTime? lockInDate { get; set; }

	// Foreign Keys
	public ICollection<ExamSlot> ExamSlots { get; set; } = [];
	public Classroom Classroom { get; set; } = default!;
	public ICollection<Teacher> Teachers { get; set; } = [];
	public Subject Subject { get; set; } = default!;
}

public class ExamSlot
{
	public int Id { get; set; }

	// Foreign Keys
	public Lesson Period { get; set; } = default!;
	public Classroom Classroom { get; set; } = default!;

}