using Microsoft.DotNet.PlatformAbstractions;
using Models.API;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using Util;
using Util.DataStructures;
using Util.Extensions;
using Util.Validation;

namespace Entities;

public interface ISchedule
{
	Result TryEnlistStudent(Guid examSlotId, StudentProfile studentId);

	Result TryAddSwapRequest(SwapRequest swapRequest);
	Result TryAcceptSwapRequest(Guid swapRequestId, Guid acceptingStudentId);
	void ResolveImplicitSwaps();
	Result TryResolveImplicitSwapRequest(Guid firstSwapRequestId, Guid secondSwapRequestId);
	Result TryDeleteSwapRequest(Guid swapRequestId);

	Result TryFillSlots(IEnumerable<StudentProfile> students);
	Result TryExtend(int studentCount, out IEnumerable<ExamSlot> createdSlots);

	Result TryReportStudents(Guid examslotId, Guid teacherId, IEnumerable<StudentProfile> students, out IEnumerable<ExamSlot> createdExamSlots);
}

public class Schedule : EntityBase<Schedule>, ISchedule
{
	[Key]
	public override Guid Id { get; set; } = Guid.CreateVersion7();
	[Required]
	public Guid ClassroomId { get; set; }
	[Required]
	public required Classroom Classroom { get; set; }
	[Required]
	public required DateTimeOffset StartDate { get; set; }
	[NotMapped]
	public DateTimeOffset EndDate => ExamSlots.Order().LastOrDefault()?.Date ?? StartDate;
	//[Required, DefinedEnum]
	//public required AutoLockIn AutoLockIn { get; set; } = AutoLockIn.TimeBeforeExamination;
	// AutoLockIn.FixedDate = StartDate + AutoLockInOffset
	// AutoLockIn.TimeBeforeExamination = Examslot.Date - AutoLockInOffset 
	[Required, PositiveTimeSpan]
	public required TimeSpan AutoLockInOffset { get; set; } = TimeSpan.Zero; // offset into the past from the date of the examination
	public string? Description { get; set; }
	//[Required, DefinedEnum]
	//public required SlotFillingBehaviour SlotFillingBehaviour { get; set; }

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
	public int MaxParticipants => ExamSlots.Sum(s => s.IsLocked ? s.Participants.Count : s.MaxParticipants);


	[Timestamp]
	public override uint Version { get; set; }

	public Result TryReportStudents(Guid examslotId, Guid teacherProfileId, IEnumerable<StudentProfile> students, out IEnumerable<ExamSlot> createdExamslots)
	{
		createdExamslots = [ ];

		var teacher = Teachers
			.Where(t => t.TeacherProfileId == teacherProfileId)
			.FirstOrDefault();
		if (teacher is null)
		{
			return new(HttpStatusCode.NotFound, "Teacher not found");
		}

		var studentsInSameSchedule = students
			.All(s => ExamSlots
				.SelectMany(e => e.Participants)
				.Any(p => p.Id == s.Id)
			);
		if (!studentsInSameSchedule)
		{
			return new(HttpStatusCode.BadRequest, "Not all students are in the schedule"); // BUG
		}

		var totalStudentCount = ExamSlots.Sum(s => s.Participants.Count);

		foreach (var iterSlot in ExamSlots)
		{
			var studentsToRemove = iterSlot.Participants.Intersect(students);
			iterSlot.Participants.RemoveRange(studentsToRemove.ToArray());
		}

		var slot = ExamSlots.FindById(examslotId);
		if (slot is null)
		{
			return new(HttpStatusCode.NotFound, "Slot not found");
		}

		var slotReportResult = slot.TryReportStudents(students, out var previousStudents);
		if (!slotReportResult.Success)
		{
			return slotReportResult;
		}
		slot.IsTeacherConfirmed = true;
		slot.MaxParticipants = students.Count();

		var extendResult = TryExtend(totalStudentCount, out createdExamslots);
		if (!extendResult.Success)
		{
			return extendResult;
		}
		var fillResult = TryFillSlots(previousStudents);
		if (!fillResult.Success)
		{
			return fillResult;
		}

		AuditLogs.Add(new()
		{
			Action = AuditLogAction.ReportStudents,
			OriginType = AuditLogActor.Teacher,
			OriginId = teacherProfileId,
			OriginName = teacher.Name,
			TargetType = AuditLogTarget.Schedule,
			TargetId = Id,
		});
		return new(HttpStatusCode.OK);
	}

	public Result TryFillSlots(IEnumerable<StudentProfile> students)
	{
		var studentsNotYetEnlisted = students.Except(ExamSlots.SelectMany(s => s.Participants)).ToList();
		if (studentsNotYetEnlisted.Count == 0)
		{
			return new(HttpStatusCode.OK);
		}

		var slotsToFill = ExamSlots
			.Where(s => s.ShouldBeFilled)
			.Order()
			.ToList();

		for (var i = 0; i < slotsToFill.Count; i++)
		{
			var slot = slotsToFill[ i ];
			var tempStudents = studentsNotYetEnlisted.OrderById().Take(slot.MaxParticipants - slot.Participants.Count);
			slot.Participants.AddRange(tempStudents);
			slot.HasBeenAutoFilled = true;
			studentsNotYetEnlisted.RemoveRange(tempStudents.ToArray());
			if (studentsNotYetEnlisted.Count == 0)
			{
				return new(HttpStatusCode.OK);
			}
		}

		return new(HttpStatusCode.BadRequest, "Too little slots available");
	}

	public Result TryExtend(int studentCount, out IEnumerable<ExamSlot> createdSlots)
	{
		DateTimeOffset GetLockInDate(DateTimeOffset slotDate)
		{
			return slotDate - AutoLockInOffset;
			//return AutoLockIn switch
			//{
			//	AutoLockIn.FixedDate => new DateTimeOffset(StartDate.ToDateTime(TimeOnly.MinValue), AutoLockInOffset),
			//	AutoLockIn.TimeBeforeExamination => new DateTimeOffset(slotDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) - AutoLockInOffset,
			//	_ => DateTimeOffset.MinValue
			//};
		}

		createdSlots = [ ];
		var result = new List<ExamSlot>();
		var nextDate = EndDate;
		foreach (var generatorSlot in LoopingEnumerable.From(ScheduleGenerator.GeneratorSlots.Order()))
		{
			if (MaxParticipants >= studentCount)
			{
				break;
			}

			nextDate = nextDate.RoundUpTo(generatorSlot.DayOfWeek, false);
			if (ScheduleGenerator.BlacklistedDays.Contains(nextDate))
			{
				continue;
			}

			var newSlot = new ExamSlot()
			{
				ScheduleId = Id,
				IsGenerated = true,
				Date = nextDate,
				LockInDate = GetLockInDate(nextDate),
				MaxParticipants = generatorSlot.MaxParticipants,
			};
			result.Add(newSlot);
			ExamSlots.Add(newSlot);
		}

		if (MaxParticipants >= studentCount)
		{
			createdSlots = result;
			return new(HttpStatusCode.OK);
		}
		return new(HttpStatusCode.BadRequest);
	}

	public Result TryEnlistStudent(Guid examslotId, StudentProfile student)
	{
		var previousSlot = ExamSlots
			.Where(s => s.Participants.Contains(student))
			.FirstOrDefault();
		if (previousSlot is not null && previousSlot.IsLocked)
		{
			return new(HttpStatusCode.Unauthorized, "Student is already enlisted in a locked slot");
		}

		var slot = ExamSlots.FindById(examslotId);
		if (slot is null)
		{
			return new(HttpStatusCode.NotFound, "Slot not found");
		}

		var enlistResult = slot.TryEnlistStudent(student);
		if (!enlistResult.Success)
		{
			return enlistResult;
		}

		AuditLogs.Add(new()
		{
			Action = AuditLogAction.EnlistInExamslot,
			OriginId = student.Id,
			OriginName = student.UserProfile.Name,
			OriginType = AuditLogActor.Student,
			TargetId = examslotId,
			TargetType = AuditLogTarget.ExamSlot
		});
		return new(HttpStatusCode.OK);
	}

	public Result TryAddSwapRequest(SwapRequest swapRequest)
	{
		var slot = ExamSlots.FindById(swapRequest.RequestedSlotId);
		var requestingStudent = ExamSlots.SelectMany(s => s.Participants).FindById(swapRequest.RequestingStudentId);
		if (slot is null)
		{
			return new(HttpStatusCode.NotFound, "Slot not found");
		}
		if (requestingStudent is null)
		{
			return new(HttpStatusCode.NotFound, "Requesting student not found");
		}
		if (Id != swapRequest.ScheduleId)
		{
			return new(HttpStatusCode.Unauthorized);
		}
		SwapRequests.Add(swapRequest);
		AuditLogs.Add(new()
		{
			Action = AuditLogAction.CreateSwapRequest,
			OriginType = AuditLogActor.Student,
			OriginId = swapRequest.RequestedSlotId,
			OriginName = swapRequest.RequestingStudentName,
			TargetType = AuditLogTarget.SwapRequest,
			TargetId = swapRequest.Id
		});
		return new(HttpStatusCode.OK);
	}

	public Result TryAcceptSwapRequest(Guid swapRequestId, Guid acceptingStudentId)
	{
		var swapRequest = SwapRequests.FindById(swapRequestId);
		if (swapRequest is null)
		{
			return new(HttpStatusCode.NotFound, "Swaprequest not found");
		}

		var acceptingStudent = ExamSlots.SelectMany(s => s.Participants).FindById(acceptingStudentId);
		if (acceptingStudent is null)
		{
			return new(HttpStatusCode.NotFound, "Accepting student not enlisted in schedule");
		}
		var acceptingStudentSlot = ExamSlots.FirstOrDefault(s => s.Participants.Any(p => p.Id == acceptingStudentId));
		if (acceptingStudentSlot is null || acceptingStudentSlot.Id != swapRequest.RequestedSlotId)
		{
			return new(HttpStatusCode.BadRequest);
		}

		var requestingStudentSlot = ExamSlots.FirstOrDefault(s => s.Participants.Any(p => p.Id == swapRequest.RequestingStudentId));
		if (requestingStudentSlot is null || requestingStudentSlot.Id == acceptingStudentSlot.Id)
		{
			return new(HttpStatusCode.BadRequest);
		}

		var result = TrySwapStudents(acceptingStudentId, swapRequest.RequestingStudentId);
		if (result.Success)
		{
			SwapRequests.Remove(swapRequest);

			AuditLogs.Add(new()
			{
				Action = AuditLogAction.AcceptSwapRequest,
				OriginType = AuditLogActor.Student,
				TargetType = AuditLogTarget.Student,
				OriginId = swapRequest.RequestingStudentId,
				TargetId = acceptingStudentId,
				OriginName = swapRequest.RequestingStudentName,
				TargetName = acceptingStudent.UserProfile.Name,
			});
		}
		return result;
	}

	// could be moved to an event
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

	public Result TryResolveImplicitSwapRequest(Guid firstSwapRequestId, Guid secondSwapRequestId)
	{
		var firstSwapRequest = SwapRequests.FindById(firstSwapRequestId);
		var secondSwapRequest = SwapRequests.FindById(secondSwapRequestId);
		if (firstSwapRequest is null || secondSwapRequest is null)
		{
			return new(HttpStatusCode.NotFound);
		}

		var result = TrySwapStudents(firstSwapRequest.RequestingStudentId, secondSwapRequest.RequestingStudentId);
		if (result.Success)
		{
			SwapRequests.RemoveRange(firstSwapRequest, secondSwapRequest);

			AuditLogs.Add(new()
			{
				Action = AuditLogAction.AcceptSwapRequest,
				OriginType = AuditLogActor.Student,
				TargetType = AuditLogTarget.Student,
				OriginId = firstSwapRequest.RequestingStudentId,
				TargetId = secondSwapRequest.RequestingStudentId,
				OriginName = firstSwapRequest.RequestingStudentName,
				TargetName = secondSwapRequest.RequestingStudentName,
			});
		}
		return result;
	}

	public Result TryDeleteSwapRequest(Guid swapRequestId)
	{
		var swapRequest = SwapRequests.FindById(swapRequestId);
		if (swapRequest is null)
		{
			return new(HttpStatusCode.BadRequest);
		}
		SwapRequests.Remove(swapRequest);
		AuditLogs.Add(new()
		{
			Action = AuditLogAction.DeleteSwapRequest,
			OriginType = AuditLogActor.Student,
			OriginId = swapRequest.RequestingStudentId,
			OriginName = swapRequest.RequestingStudentName,
			TargetType = AuditLogTarget.SwapRequest,
			TargetId = swapRequest.Id
		});
		return new(HttpStatusCode.OK);
	}

	private Result TrySwapStudents(Guid firstStudentId, Guid secondStudentId)
	{
		var participants = ExamSlots
			.Where(e => !e.IsLocked)
			.SelectMany(e => e.Participants)
			.ToList();

		var firstStudent = participants.FindById(firstStudentId);
		var secondStudent = participants.FindById(secondStudentId);
		if (firstStudent is null || secondStudent is null)
		{
			return new(HttpStatusCode.NotFound);
		}

		return TrySwapStudents(firstStudent, secondStudent);
	}

	private Result TrySwapStudents(StudentProfile firstStudent, StudentProfile secondStudent)
	{
		var firstStudentExamSlot = ExamSlots.FirstOrDefault(s => !s.IsLocked && s.Participants.Contains(firstStudent));
		if (firstStudentExamSlot is null)
		{
			return new(HttpStatusCode.NotFound);
		}
		var secondStudentExamSlot = ExamSlots.FirstOrDefault(s => !s.IsLocked && s.Participants.Contains(secondStudent));
		if (secondStudentExamSlot is null)
		{
			return new(HttpStatusCode.NotFound);
		}

		if (firstStudentExamSlot.Id != secondStudentExamSlot.Id)
		{
			return new(HttpStatusCode.Unauthorized);
		}

		return firstStudentExamSlot.TrySwapStudents(firstStudent, secondStudent).MergeErrors(secondStudentExamSlot.TrySwapStudents(secondStudent, firstStudent));
	}

	public override bool EqualsCore(Schedule b) =>
		StartDate == b.StartDate &&
		EndDate == b.EndDate &&
		//AutoLockIn == b.AutoLockIn &&
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
		//combiner.Add(AutoLockIn);
		combiner.Add(AutoLockInOffset);
		combiner.Add(Description);
		combiner.Add(Subject);
		combiner.Add(ExamSlots.GetValueHashCode());
		combiner.Add(AuditLogs.GetValueHashCode());
		combiner.Add(SwapRequests.GetValueHashCode());
		return combiner.CombinedHash;
	}

	public override int CompareTo(Schedule? other) => StartDate.CompareTo(other?.StartDate ?? DateTimeOffset.MinValue);
}
