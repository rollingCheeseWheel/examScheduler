namespace ExamScheduler.Entities;

public class Student
{
	public int Id { get; set; }
	public string RegisterUsername { get; set; } = default!;
	public Uri RegisterUri { get; set; } = default!;
	public string Name { get; set; } = default!;
	public string Surname { get; set; } = default!;
	public string DisplayName { get; set; } = default!;
	public DateTime CreatedAt { get; set; }

	public string Salt { get; set; } = default!;
	public string Hash { get; set; } = default!;

	// Permissions - enum flags, can be combined
	// e.g. Permission.Read | Permission.Write = 3
	public Permission Permissions { get; set; } = default!;

	// Navigation Properties
	public Classroom Classroom { get; set; } = default!;
}
