using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.DigitalesRegister;

namespace Entities;

public class Schedule
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

	public AutoLockIn AutoLockIn { get; set; } = AutoLockIn.OnExamination;
	public DateTime? lockInDate { get; set; }

	// Navigation Properties
	[Required] // not null
	public ICollection<ExamSlot> ExamSlots { get; set; } = [ ];
	[Required]
	public required Classroom Classroom { get; set; }
	[Required]
	public ICollection<Teacher> Teachers { get; set; } = [ ];
	[Required]
	public required Subject Subject { get; set; }
}

public class ExamSlot
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

	// Navigation Properties
	[Required]
	public required Lesson Period { get; set; }
	[Required]
	public required Classroom Classroom { get; set; }

}