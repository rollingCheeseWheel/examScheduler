using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SqlTypes;

namespace Entities;

public class Schedule
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

	[NotMapped]
	public int RequiredParticipants { get => ExamSlots.Select(e => e.RequiredParticipants).Sum(); }
	[NotMapped]
	public int Participants { get => ExamSlots.Select(e => e.Participating).Sum(); }

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

	public bool TrySwapStudents(Student student1, Student student2)
	{
		if (ExamSlots.Count == 0)
		{
			return false;
		}
		if (student1 == student2)
		{
			return true;
		}

		var slot1 = GetExamSlot(student1);
		var slot2 = GetExamSlot(student2);
		if (slot1 is null || slot2 is null)
		{
			return false;
		}
		else if (slot1.AlreadyHappened || slot2.AlreadyHappened)
		{
			return false;
		}
		else
		{
			return slot1.TrySwapStudents(student1, student2)
				&& slot2.TrySwapStudents(student2, student1);
		}
	}

	public ExamSlot? GetExamSlot(Student student) => ExamSlots.Where(e => e.IsParticipating(student)).FirstOrDefault();

	public bool TryEnlistStudent(Student student, DateTime date)
	{
		throw new NotImplementedException();
	}

	public bool TryEnlistStudent(Student student, ExamSlot slot)
	{
		throw new NotImplementedException();
	}

	public bool TryEnlistStudentAtNearestDate(Student student)
	{
		if (GetExamSlot(student) is not null) // student is already enlisted 
		{
			return true;
		}
		var nextOpenExamSlot = GetNextOpenExamSlot();
		if (nextOpenExamSlot is null)
		{
			return false;
		}

		return nextOpenExamSlot.TryEnlistStudent(student);
	}

	public bool EnlistStudentAtNearesDateForcefully(Student student)
	{

		throw new NotImplementedException();
	}

	public ExamSlot? GetNextOpenExamSlot()
	{
		return ExamSlots
			.Where(e => !e.IsFull())
			.OrderBy(e => e.Participating)
			.ThenBy(e => e.Date)
			.ThenByDescending(e => e.RequiredParticipants)
			.FirstOrDefault();
	}

	public ExamSlot? GetNextOpenExamSlotForcefully()
	{
		return ExamSlots
			.Select((e, index) => new { e, index })
			.OrderBy(a => a.index)
			.Select(a => a.e)
			.Where(e => !e.IsFull())
			.OrderBy(e => e.Participating)
			.ThenBy(e => e.Date)
			.ThenByDescending(e => e.RequiredParticipants)
			.FirstOrDefault();
	}
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
	[Required]
	public required bool AlreadyHappened { get; set; }

	// Navigation Properties
	[Required]
	public required ICollection<Student> Participants { get; set; } = [ ];
	[Required]
	public required Schedule Schedule { get; set; }

	[NotMapped]
	public int Participating { get => Participants.Count; }

	internal bool TrySwapStudents(Student target, Student replacement)
	{
		if (!IsParticipating(target))
		{
			return false;
		}
		else if (!Participants.Remove(target))
		{
			return false;
		}
		else
		{
			Participants.Add(replacement);
			return true;
		}
	}

	public bool IsParticipating(Student student) => Participants.Contains(student);

	public bool TryEnlistStudent(Student student)
	{
		if (IsParticipating(student))
		{
			return false;
		}
		else if (IsFull())
		{
			return false;
		}
		else
		{
			Participants.Add(student);
			return true;
		}
	}

	public bool IsFull() => Participating >= MaxParticipants;

	public int GetMissingParticipantCount() => Math.Max(Participating - RequiredParticipants, 0);
}