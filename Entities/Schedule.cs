using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities;

public class Schedule
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

	[NotMapped]
	public int RequiredParticipants { get => ExamSlots.Select(e => e.RequiredParticipants).Sum(); }
	[NotMapped]
	public int Participants { get => ExamSlots.Select(e => e.Participants.Count).Sum(); }

	[Required]
	public required AutoLockIn AutoLockIn { get; set; } = AutoLockIn.OnExamination;
	// AutoLockIn.FixedDate = the date the freeze happens
	// AutoLockIn.TimeBeforeExamination = offset from DateTime.MinValue represents how much time before the exam slot it locks in
	public DateTime? lockInDate { get; set; }

	// Navigation Properties
	[Required] // not null
	public required ICollection<ExamSlot> ExamSlots { get; set; } = [ ];
	[Required]
	public required Classroom Classroom { get; set; }
	[Required]
	public required ICollection<Teacher> Teachers { get; set; } = [ ];
	[Required]
	public required Subject Subject { get; set; }
}

public class ExamSlot
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

	[Required]
	[Range(0, int.MaxValue)]
	public required int RequiredParticipants { get; set; }
	[Required]
	[Range(0, int.MaxValue)]
	public required int MaxParticipants { get; set; }
	[Required]
	public required DateTime Date { get; set; }
	[Required]
	public required bool IsLocked { get; set; }

	// Navigation Properties
	[Required]
	public required ICollection<Student> Participants { get; set; } = [ ];
}