using Microsoft.DotNet.PlatformAbstractions;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Util;
using Util.Extensions;
using Util.Validation;

namespace Entities;

public interface ISchedule
{
	bool TryEnlistStudent(Guid examSlotId, StudentProfile studentId);

	bool TryAddSwapRequest(SwapRequest swapRequest);
	bool TryAcceptSwapRequest(Guid swapRequestId, Guid acceptingStudentId);
	bool TryDeleteSwapRequest(Guid swapRequestId, Guid? actingStudent);

	bool TryReportStudents(Guid studentId, params StudentProfile[ ] students);
}

public class Schedule : EntityBase<Schedule>, ISchedule
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
	[Required]
	public ICollection<ExamSlot> ExamSlots { get; private set; } = [ ];
	[Required]
	public ICollection<AuditLog> AuditLogs { get; private set; } = [ ];
	[Required]
	public ICollection<SwapRequest> SwapRequests { get; set; } = [ ];

	[Timestamp]
	public override uint Version { get; set; }

	public bool TryReportStudents(Guid examslotId, params StudentProfile[ ] students)
	{
		var slot = ExamSlots.FindById(examslotId);
		if (slot is null)
		{
			return false;
		}
		var isSuccess = slot.TryReportStudents(students);
		if (isSuccess)
		{
			AuditLogs.Add(new()
			{
				Action = AuditLogAction.ReportStudents,
				ActorType = AuditLogActor.Teacher,
			});
		}
		return isSuccess;
	}

	public bool TryEnlistStudent(Guid examslotId, StudentProfile student)
	{
		var slot = ExamSlots.FirstOrDefault(s => s.Id == examslotId);
		return slot?.TryEnlistStudent(student) ?? false;
	}

	public bool TryAddSwapRequest(SwapRequest swapRequest)
	{
		var slot = ExamSlots.FindById(swapRequest.RequestedSlotId);
		var requestingStudent = ExamSlots.SelectMany(s => s.Participants).FindById(swapRequest.RequestingStudentId);

		if (slot is null || requestingStudent is null || Id != swapRequest.ScheduleId)
		{
			return false;
		}
		SwapRequests.Add(swapRequest);
		AuditLogs.Add(new()
		{
			Action = AuditLogAction.CreateSwapRequest,
			ActorType = AuditLogActor.Student,
			ActorId = swapRequest.RequestedSlotId,
			ActorName = swapRequest.RequestingStudentName
		});
		return true;
	}

	public bool TryAcceptSwapRequest(Guid swapRequestId, Guid acceptingStudentId)
	{
		var swapRequest = SwapRequests.FindById(swapRequestId);
		if (swapRequest is null)
		{
			return false;
		}
		var isSuccess = TrySwapStudents(acceptingStudentId, swapRequest.RequestingStudentId);
		if (isSuccess)
		{
			AuditLogs.Add(new()
			{
				Action = AuditLogAction.AcceptSwapRequest,
				ActorType = AuditLogActor.Student,
				ActorId = swapRequest.RequestedSlotId,
			});
		}
		return isSuccess;
	}
	public bool TryDeleteSwapRequest(Guid swapRequestId)
	{
		var swapRequest = SwapRequests.FindById(swapRequestId);
		if (swapRequest is null)
		{
			return false;
		}
		SwapRequests.Remove(swapRequest);
		AuditLogs.Add(new()
		{
			Action = AuditLogAction.DeleteSwapRequest,
			ActorType = AuditLogActor.Student,
			ActorId = swapRequest.RequestedSlotId,
		});
		return true;
	}

	private bool TrySwapStudents(Guid firstStudentId, Guid secondStudentId)
	{
		var participants = ExamSlots
			.Where(e => !e.IsLocked)
			.SelectMany(e => e.Participants)
			.ToList();

		var firstStudent = participants.FindById(firstStudentId);
		var secondStudent = participants.FindById(secondStudentId);
		if (firstStudent is null || secondStudent is null)
		{
			return false;
		}
		return TrySwapStudents(firstStudent, secondStudent);
	}

	private bool TrySwapStudents(StudentProfile firstStudent, StudentProfile secondStudent)
	{
		var firstStudentExamSlot = ExamSlots.FirstOrDefault(s => !s.IsLocked && s.Participants.Contains(firstStudent));
		var secondStudentExamSlot = ExamSlots.FirstOrDefault(s => !s.IsLocked && s.Participants.Contains(secondStudent));

		var isSuccess = firstStudentExamSlot is not null &&
			 secondStudentExamSlot is not null &&
			 firstStudentExamSlot.Id != secondStudentExamSlot.Id &&
			 firstStudentExamSlot.TrySwapStudents(firstStudent, secondStudent) &&
			 secondStudentExamSlot.TrySwapStudents(secondStudent, firstStudent);
		if (isSuccess)
		{
			AuditLogs.Add(new()
			{
				Action = AuditLogAction.swap
			})
		}
		return isSuccess;
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
