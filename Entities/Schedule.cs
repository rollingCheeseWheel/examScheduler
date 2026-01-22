using System.ComponentModel.DataAnnotations;
using Util;

namespace Entities;

public class Schedule : EntityBase<Schedule>
{
	[Key]
	public override Guid Id { get; set; } = Guid.NewGuid();
	[Required]
	public required DateTimeOffset FirstExamination { get; set; }
	[Required]
	public required AutoLockIn AutoLockIn { get; set; } = AutoLockIn.TimeBeforeExamination;
	// AutoLockIn.FixedDate = FirstExamination - LockInDate
	// AutoLockIn.TimeBeforeExamination = Offset, slot locks at this offset before the examination 
	[Required]
	public required TimeSpan LockInOffset { get; set; } = TimeSpan.Zero; // offset into the past from the date of the examination
	[Required]
	public required string Description { get; set; }

	// Navigation properties
	[Required]
	public required ICollection<ScheduleGeneratorSlot> GeneratorSlots { get; set; }
	[Required]
	public required Subject Subject { get; set; }
	//[Required]
	//public required Classroom Classroom { get; set; }
	[Required]
	public ICollection<ExamSlot> ExamSlots { get; private set; } = [ ];
	[Required]
	public ICollection<AuditLog> AuditLogs { get; private set; } = [ ];
	[Required]
	public ICollection<SwapRequest> SwapRequests { get; private set; } = [ ];

	public bool TrySwapStudents(StudentProfile firstStudent, StudentProfile secondStudent)
	{
		ExamSlot? firstStudentExamSlot = ExamSlots.FirstOrDefault(s => !s.IsLocked && s.Participants.Contains(firstStudent));
		ExamSlot? secondStudentExamSlot = ExamSlots.FirstOrDefault(s => !s.IsLocked && s.Participants.Contains(secondStudent));
		return firstStudentExamSlot is not null &&
			 secondStudentExamSlot is not null &&
			  firstStudentExamSlot.Id != secondStudentExamSlot.Id && firstStudentExamSlot.TrySwapStudents(firstStudent, secondStudent) &&
			 secondStudentExamSlot.TrySwapStudents(secondStudent, firstStudent);
	}

	public bool TryEnlistStudent(Guid examslotId, StudentProfile student)
	{
		ExamSlot? slot = ExamSlots.FirstOrDefault(s => s.Id == examslotId);
		return slot?.TryEnlistStudent(student) ?? false;
	}

	public override bool EqualsCore(Schedule b)
	{
		return FirstExamination == b.FirstExamination &&
		Subject == b.Subject /*&&
        Classroom == b.Classroom*/;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(FirstExamination, Subject/*, Classroom*/);
	}

	public override int CompareTo(Schedule? other)
	{
		return FirstExamination.CompareTo(other?.FirstExamination ?? DateTimeOffset.MinValue);
	}
}
