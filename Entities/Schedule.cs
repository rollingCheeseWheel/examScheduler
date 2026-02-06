using Microsoft.DotNet.PlatformAbstractions;
using Models.API;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography.X509Certificates;
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

	void FillSlots(IEnumerable<StudentProfile> students);
	void Extend(int studentCount);

	bool TryReportStudents(Guid examslotId, Guid teacherId, IEnumerable<StudentProfile> students);
}

public class Schedule : EntityBase<Schedule>, ISchedule
{
	[Key]
	public override Guid Id { get; set; } = Guid.NewGuid();
	[Required]
	public required DateTimeOffset StartDate { get; set; }
	[NotMapped]
	public DateTimeOffset EndDate => ExamSlots.Order().LastOrDefault()?.Date ?? StartDate;
	[Required, DefinedEnum]
	public required AutoLockIn AutoLockIn { get; set; } = AutoLockIn.TimeBeforeExamination;
	// AutoLockIn.FixedDate = StartDate - AutoLockInOffset
	// AutoLockIn.TimeBeforeExamination = Examslot.Date - AutoLockInOffset 
	[Required, PositiveTimeSpan]
	public required TimeSpan AutoLockInOffset { get; set; } = TimeSpan.Zero; // offset into the past from the date of the examination
	public string? Description { get; set; }
	[Required, DefinedEnum]
	public required SlotFillingBehaviour SlotFillingBehaviour { get; set; }

	[Required]
	public required ScheduleGenerator ScheduleGenerator { get; set; }

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

	[NotMapped]
	public int ParticipantCount => ExamSlots.Sum(s => s.Participants.Count);
	[NotMapped]
	public int MaxParticipants => ExamSlots.Sum(s => s.MaxParticipants);
	[NotMapped]
	public int MinParticipants => ExamSlots.Sum(s => s.MinParticipants);


	[Timestamp]
	public override uint Version { get; set; }

	public bool TryReportStudents(Guid examslotId, Guid teacherId, IEnumerable<StudentProfile> students)
	{
		var teacher = Teachers.FindById(teacherId);
		if (teacher is null)
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
				OriginType = AuditLogActor.Teacher,
				OriginId = teacherId,
				OriginName = teacher.Name,
			});
		}
		return isSuccess;
	}

	public void FillSlots(IEnumerable<StudentProfile> students)
	{
		var studentsNotYetEnlisted = students.Except(ExamSlots.SelectMany(s => s.Participants)).ToList();
		if (studentsNotYetEnlisted.Count == 0)
		{
			return;
		}

		var slotsToFill = ExamSlots
			.Where(s => s.ShouldBeFilled)
			.Order()
			.ToList();

		for (var i = 0; i < slotsToFill.Count; i++)
		{
			var slot = slotsToFill[ i ];
			if (studentsNotYetEnlisted.Count == 0)
			{
				break;
			}
			var tempStudents = studentsNotYetEnlisted.Take(slot.MaxParticipants - slot.Participants.Count);
			slot.Participants.AddRange(tempStudents);
			studentsNotYetEnlisted.RemoveRange(tempStudents);
		}
	}

	public void Extend(int studentCount)
	{
		DateTimeOffset GetLockInDate(DateTimeOffset slotDate)
		{
			return AutoLockIn switch
			{
				AutoLockIn.FixedDate => StartDate + AutoLockInOffset,
				AutoLockIn.TimeBeforeExamination => slotDate - AutoLockInOffset,
				_ => DateTimeOffset.MinValue
			};
		}

		var nextDate = EndDate;
		foreach (var generatorSlot in ScheduleGenerator.GetLoopingEnumerable(200))
		{
			if (ParticipantCount >= studentCount)
			{
				break;
			}

			nextDate = nextDate.RoundUpTo(generatorSlot.DayOfWeek);
			if (ScheduleGenerator.BlacklistedDays.Contains(nextDate))
			{
				continue;
			}


			ExamSlots.Add(new()
			{
				ScheduleId = Id,
				IsPostGenerated = true,
				Date = nextDate,
				LockInDate = GetLockInDate(nextDate),
				MinParticipants = generatorSlot.MinParticipants,
				MaxParticipants = generatorSlot.MaxParticipants,
			});
		}
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
			OriginType = AuditLogActor.Student,
			OriginId = swapRequest.RequestedSlotId,
			OriginName = swapRequest.RequestingStudentName
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
				OriginType = AuditLogActor.Student,
				OriginId = swapRequest.RequestingStudentId,
				TargetId = acceptingStudentId,
				OriginName = swapRequest.RequestingStudentName,
				TargetName = acceptingStudent.UserProfile.Name,
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
				OriginType = AuditLogActor.Student,
				OriginId = firstSwapRequest.RequestingStudentId,
				TargetId = secondSwapRequest.RequestingStudentId,
				OriginName = firstSwapRequest.RequestingStudentName,
				TargetName = secondSwapRequest.RequestingStudentName,
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
			OriginType = AuditLogActor.Student,
			OriginId = swapRequest.RequestingStudentId,
			OriginName = swapRequest.RequestingStudentName,
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
