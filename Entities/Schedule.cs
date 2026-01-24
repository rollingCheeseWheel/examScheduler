using Microsoft.DotNet.PlatformAbstractions;
using System.ComponentModel.DataAnnotations;
using Util;
using Util.Validation;

namespace Entities;

public class Schedule : EntityBase<Schedule>
{
	[Key]
	public override Guid Id { get; set; } = Guid.NewGuid();
	[Required]
	public required DateTimeOffset StartDate { get; set; }
	[Required]
	public required DateTimeOffset EndDate { get; set; }
	[Required, ValidEnum]
	public required AutoLockIn AutoLockIn { get; set; } = AutoLockIn.TimeBeforeExamination;
	// AutoLockIn.FixedDate = StartDate - AutoLockInOffset
	// AutoLockIn.TimeBeforeExamination = Examslot.Date - AutoLockInOffset 
	[Required, PositiveTimeSpan]
	public required TimeSpan AutoLockInOffset { get; set; } = TimeSpan.Zero; // offset into the past from the date of the examination
	public string? Description { get; set; }
	[Required, ValidEnum]
	public required SlotFillingBehaviour SlotFillingBehaviour { get; set; }

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
	public ICollection<SwapRequest> SwapRequests { get; set; } = [ ];

	public bool TryEnlistStudent(Guid examslotId, StudentProfile student)
	{
		var slot = ExamSlots.FirstOrDefault(s => s.Id == examslotId);
		return slot?.TryEnlistStudent(student) ?? false;
	}

	public bool TrySwapStudents(StudentProfile firstStudent, StudentProfile secondStudent)
	{
		var firstStudentExamSlot = ExamSlots.FirstOrDefault(s => !s.IsLocked && s.Participants.Contains(firstStudent));
		var secondStudentExamSlot = ExamSlots.FirstOrDefault(s => !s.IsLocked && s.Participants.Contains(secondStudent));
		return firstStudentExamSlot is not null &&
			 secondStudentExamSlot is not null &&
			 firstStudentExamSlot.Id != secondStudentExamSlot.Id &&
			 firstStudentExamSlot.TrySwapStudents(firstStudent, secondStudent) &&
			 secondStudentExamSlot.TrySwapStudents(secondStudent, firstStudent);
	}

	public override bool EqualsCore(Schedule b) =>
		StartDate == b.StartDate &&
		EndDate == b.EndDate &&
		AutoLockIn == b.AutoLockIn &&
		AutoLockInOffset == b.AutoLockInOffset &&
		Description == b.Description &&
		Subject == b.Subject &&
		GeneratorSlots.ValueEquals(b.GeneratorSlots) &&
		ExamSlots.ValueEquals(b.ExamSlots) &&
		AuditLogs.ValueEquals(b.AuditLogs) &&
		SwapRequests.ValueEquals(b.SwapRequests);

	public override int GetHashCode()
	{
		var combiner = new HashCodeCombiner();
		combiner.Add(StartDate);
		combiner.Add(EndDate);
		combiner.Add(AutoLockIn);
		combiner.Add(AutoLockInOffset);
		combiner.Add(Description);
		combiner.Add(Subject);
		combiner.Add(GeneratorSlots.Order());
		combiner.Add(ExamSlots.Order());
		combiner.Add(AuditLogs.Order());
		combiner.Add(SwapRequests.Order());
		return combiner.CombinedHash;
	}

	public override int CompareTo(Schedule? other) => StartDate.CompareTo(other?.StartDate ?? DateTimeOffset.MinValue);
}
