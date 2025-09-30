using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace examScheduler.Entities;

public class Schedule
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

	public AutoLockIn AutoLockIn { get; set; } = AutoLockIn.OnExamination;
	public DateTime? lockInDate { get; set; }

	// Navigation Properties
	public ICollection<ExamSlot> ExamSlots { get; set; } = [];
	[Required]
	public required Classroom Classroom { get; set; }
	public ICollection<Teacher> Teachers { get; set; } = [];
	[Required]
	public required Subject Subject { get; set; }
}

public class ExamSlot
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

	// Navigation Properties
	public required Lesson Period { get; set; }
	public required Classroom Classroom { get; set; }

}