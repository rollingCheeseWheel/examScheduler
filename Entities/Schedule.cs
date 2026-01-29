using Microsoft.DotNet.PlatformAbstractions;
using Models.API;
using System.ComponentModel.DataAnnotations;
using Util;
using Util.Extensions;
using Util.Validation;

namespace Entities;

public interface ISchedule
{
	bool TryEnlistStudent(Guid examSlotId, StudentProfile studentId);

	bool TryAddSwapRequest(SwapRequest swapRequest);
	bool TryAcceptSwapRequest(Guid swapRequestId, Guid acceptingStudentId);
	void ResolveImplicitSwaps();
	bool TryResolveImplicitSwapRequest(Guid firstSwapRequestId, Guid secondSwapRequestId);
	bool TryDeleteSwapRequest(Guid swapRequestId);

	bool TryReportStudents(Guid examslotId, Guid teacherId, IEnumerable<StudentProfile> students);
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
	public required Subject Subject { get; set; }
	[Required]
	public required ICollection<Teacher> Teachers { get; set; }
	[Required]
	public required ICollection<ExamSlot> ExamSlots { get; set; } = [ ];
	[Required]
	public ICollection<AuditLog> AuditLogs { get; private set; } = [ ];
	[Required]
	public ICollection<SwapRequest> SwapRequests { get; set; } = [ ];

	[Timestamp]
	public override uint Version { get; set; }

	public bool TryReportStudents(Guid examslotId, Guid teacherId, IEnumerable<StudentProfile> students)
	{
		if (!Teachers.ContainsId(teacherId))
		{
			return false;
		}

		var studentsInSameSchedule = students
			.All(s => ExamSlots
				.SelectMany(e => e.Participants)
				.ContainsId(s.Id)
			);
		if (!studentsInSameSchedule)
		{
			return false;
		}

		foreach (var iterSlot in ExamSlots)
		{
			var intersection = iterSlot.Participants.Intersect(students);
			iterSlot.Participants.RemoveRange(intersection);
		}

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
		var slot = ExamSlots.FindById(examslotId);
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
			FirstActorId = swapRequest.RequestedSlotId,
			FirstActorName = swapRequest.RequestingStudentName
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

		var acceptingStudent = ExamSlots.SelectMany(s => s.Participants).FindById(acceptingStudentId);
		if (acceptingStudent is null)
		{
			return false;
		}
		var acceptingStudentSlot = ExamSlots.FirstOrDefault(s => s.Participants.ContainsId(acceptingStudentId));
		if (acceptingStudentSlot is null || acceptingStudentSlot.Id != swapRequest.RequestedSlotId)
		{
			return false;
		}

		var requestingStudentSlot = ExamSlots.FirstOrDefault(s => s.Participants.ContainsId(swapRequest.RequestingStudentId));
		if (requestingStudentSlot is null || requestingStudentSlot.Id == acceptingStudentSlot.Id)
		{
			return false;
		}

		var isSuccess = TrySwapStudents(acceptingStudentId, swapRequest.RequestingStudentId);
		if (isSuccess)
		{
			SwapRequests.Remove(swapRequest);

			AuditLogs.Add(new()
			{
				Action = AuditLogAction.AcceptSwapRequest,
				ActorType = AuditLogActor.Student,
				FirstActorId = swapRequest.RequestingStudentId,
				SecondActorId = acceptingStudentId,
				FirstActorName = swapRequest.RequestingStudentName,
				SecondActorName = acceptingStudent.UserProfile.Name,
			});
		}
		return isSuccess;
	}

	public void ResolveImplicitSwaps()
	{
		var swapRequestAndOriginSlot = ExamSlots
			.Where(e => !e.IsLocked)
			.SelectMany(e =>
				e.Participants.Select(p => new
				{
					SlotId = e.Id,
					Participant = p
				})
			)
			.Join(SwapRequests,
				g => g.Participant.Id,
				sr => sr.RequestingStudentId,
				(g, sr) => new
				{
					g.SlotId,
					SwapRequest = sr
				})
			.ToDictionary(x => x.SlotId, x => x.SwapRequest);

		foreach (var (slotId, swapRequest) in swapRequestAndOriginSlot)
		{
			if (swapRequestAndOriginSlot.TryGetValue(slotId, out var implicitSwapRequest))
			{
				TryResolveImplicitSwapRequest(swapRequest.Id, implicitSwapRequest.Id);
			}
		}
	}

	public bool TryResolveImplicitSwapRequest(Guid firstSwapRequestId, Guid secondSwapRequestId)
	{
		var firstSwapRequest = SwapRequests.FindById(firstSwapRequestId);
		var secondSwapRequest = SwapRequests.FindById(secondSwapRequestId);
		if (firstSwapRequest is null || secondSwapRequest is null)
		{
			return false;
		}

		var success = TrySwapStudents(firstSwapRequest.RequestingStudentId, secondSwapRequest.RequestingStudentId);
		if (success)
		{
			SwapRequests.RemoveRange(firstSwapRequest, secondSwapRequest);

			AuditLogs.Add(new()
			{
				Action = AuditLogAction.AcceptSwapRequest,
				ActorType = AuditLogActor.Student,
				FirstActorId = firstSwapRequest.RequestingStudentId,
				SecondActorId = secondSwapRequest.RequestingStudentId,
				FirstActorName = firstSwapRequest.RequestingStudentName,
				SecondActorName = secondSwapRequest.RequestingStudentName,
			});
		}
		return success;
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
			FirstActorId = swapRequest.RequestingStudentId,
			FirstActorName = swapRequest.RequestingStudentName,
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
		combiner.Add(ExamSlots.Order());
		combiner.Add(AuditLogs.Order());
		combiner.Add(SwapRequests.Order());
		return combiner.CombinedHash;
	}

	public override int CompareTo(Schedule? other) => StartDate.CompareTo(other?.StartDate ?? DateTimeOffset.MinValue);
}
