using Entities;
using examScheduler.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;
using Util.Extensions;

namespace examScheduler.Services;

public interface IScheduleService
{
	Task<Schedule?> GetScheduleAsync(Guid id, CancellationToken ct = default);
	Task<Schedule?> GetScheduleForExamSlotAsync(Guid slotId, CancellationToken ct = default);
	Task<ExamSlot?> GetExamSlotAsync(Guid id, CancellationToken ct = default);
	Task<IEnumerable<Schedule>> GetSchedulesForStudentAsync_AsNoTracking(Guid userId, CancellationToken ct = default);
	Task<IEnumerable<Guid>> GetScheduleIdsForStudentAsync_AsNoTracking(Guid userId, CancellationToken ct = default);

	Task<Guid?> TryCreateSchedule(Models.API.ScheduleCreateRequest request, Guid teacherId, CancellationToken ct = default);
	Task<Guid?> TryReportActualStudentsForScheduleSlot(Guid scheduleSlotId, IEnumerable<Models.API.UserProfile> actualParticipants, CancellationToken ct = default);

	Task<Guid?> TryEnlistStudentAsync(Guid slotId, Guid actingStudentId, CancellationToken ct = default);

	Task<SwapRequest?> TryCreateSwapRequestAsync(Guid scheduleId, Guid requestingStudentId, Guid requestedSlotId, DateTimeOffset expirationDate, CancellationToken ct = default);
	Task<SwapRequest?> TryDeleteSwapRequestAsync(Guid swapRequestId, Guid actingStudentId, CancellationToken ct = default);
	Task<SwapRequest?> TryAcceptSwapRequestAsync(Guid swapRequestId, Guid actingStudentId, CancellationToken ct = default);
}

public class ScheduleService(
	AppDbContext context,
	IStudentService studentService
) : IScheduleService
{
	private readonly AppDbContext _context = context;
	private readonly IStudentService _studentService = studentService;

	public async Task<Schedule?> GetScheduleAsync(Guid id, CancellationToken ct = default) => await _context.Schedules.FindByIdAsync(id, ct);

	public async Task<Schedule?> GetScheduleForExamSlotAsync(Guid slotId, CancellationToken ct = default)
	{
		var slot = await _context.Schedules
			.SelectMany(s => s.ExamSlots)
			.FindByIdAsync(slotId, ct);
		return slot is null ? null : await GetScheduleAsync(slot.ScheduleId, ct);
	}

	public async Task<ExamSlot?> GetExamSlotAsync(Guid id, CancellationToken ct = default) => await _context.Schedules
			.SelectMany(s => s.ExamSlots)
			.FirstOrDefaultAsync(e => e.Id == id, ct);

	public async Task<IEnumerable<Schedule>> GetSchedulesForStudentAsync_AsNoTracking(Guid userId, CancellationToken ct = default) => await _context.StudentProfiles
			.AsNoTracking()
			.Where(sp => sp.Id == userId)
			.SelectMany(sp => sp.Classroom.Schedules)
			.ToListAsync(ct);

	public async Task<IEnumerable<Guid>> GetScheduleIdsForStudentAsync_AsNoTracking(Guid userId, CancellationToken ct = default) => await _context.StudentProfiles
			.AsNoTracking()
			.Where(sp => sp.Id == userId)
			.SelectMany(sp => sp.Classroom.Schedules)
			.Select(s => s.Id)
			.ToListAsync(ct);

	public async Task<Guid?> TryCreateSchedule(Models.API.ScheduleCreateRequest request, Guid teacherId, CancellationToken ct = default)
	{
		var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Name == request.SubjectName, ct);
		if (subject is null)
		{
			return null;
		}

		var teacherProfile = await _context.TeacherProfiles.FindByIdAsync( teacherId , ct);
		if (teacherProfile is null) { return null; }

		var classroom = await _context.Classrooms.FindByIdAsync(request.ClassroomId , ct);
		if (classroom is null || !teacherProfile.Classrooms.Contains(classroom))
		{
			return null;
		}

		var minCapacity = request.GeneratorSlots.Sum(g => g.MinParticipants);
		var maxCapacity = request.GeneratorSlots.Sum(g => g.MaxParticipants);
		if (classroom.Students.Count < minCapacity ||
			classroom.Students.Count > maxCapacity)
		{
			return null;
		}

		var generatorSlots = request.GeneratorSlots
			.Select(g => new ScheduleGeneratorSlot
			{
				MaxParticipants = g.MaxParticipants,
				MinParticipants = g.MinParticipants,
				Offset = g.Offset,
			})
			.ToList();

		var newSchedule = new Schedule
		{
			SlotFillingBehaviour = request.SlotFillingBehaviour,
			AutoLockIn = request.AutoLockIn,
			AutoLockInOffset = request.LockInOffset,
			StartDate = request.StartDate,
			EndDate = request.EndDate,
			GeneratorSlots = generatorSlots,
			Subject = subject,
		};

		classroom.Schedules.Add(newSchedule);
		_context.Schedules.Add(newSchedule);
		await _context.SaveChangesAsync(ct);

		return newSchedule.Id;
	}

	public async Task<Guid?> TryReportActualStudentsForScheduleSlot(Guid scheduleSlotId, IEnumerable<Models.API.UserProfile> participants, CancellationToken ct = default)
	{
		var slot = await GetExamSlotAsync(scheduleSlotId, ct);
		if (slot is null)
		{
			return null;
		}

		var students = await DoAllStudentsExistAsync(participants.Select(x => x.Id), ct);
		if (students is null || !students.Any() || !StudentsInSameSchedule(students, slot.ScheduleId))
		{
			return null;
		}

		slot.Participants.Clear();
		slot.Participants.AddRange(students);

		await _context.SaveChangesAsync(ct);
		return slot.Id;
	}

	public async Task<Guid?> TryEnlistStudentAsync(Guid slotId, Guid studentId, CancellationToken ct = default)
	{
		var student = await _studentService.GetStudentProfileAsync(studentId, ct);
		if (student is null)
		{
			return null;
		}

		var schedule = await GetScheduleForExamSlotAsync(slotId, ct);
		if (schedule is null)
		{
			return null;
		}

		if (schedule.TryEnlistStudent(slotId, student))
		{
			await _context.SaveChangesAsync(ct);
			return schedule.Id;
		}
		return null;
	}

	public async Task<SwapRequest?> TryCreateSwapRequestAsync(Guid scheduleId, Guid requestingStudentId, Guid requestedSlotId, DateTimeOffset expirationDate, CancellationToken ct = default)
	{
		var schedule = await _context.Schedules.FindByIdAsync(scheduleId, ct);
		if (schedule is null)
		{
			return null;
		}

		var existingSwapRequest = schedule.SwapRequests
			.Where(sr => sr.RequestingStudentId == requestingStudentId)
			.Where(sr => sr.RequestedSlotId == requestedSlotId)
			.FirstOrDefault();
		if (existingSwapRequest is not null)
		{
			return null;
		}

		var examslot = schedule.ExamSlots.FindById(requestedSlotId);
		if (examslot is null)
		{
			return null;
		}

		var requestingStudent = await _context.StudentProfiles.FindByIdAsync(requestingStudentId, ct);
		if (requestingStudent is null)
		{
			return null;
		}

		var newSwapRequest = new SwapRequest
		{
			ScheduleId = scheduleId,
			RequestingStudentName = requestingStudent.UserProfile.Name,
			RequestingStudentId = requestingStudentId,
			RequestedSlotId = requestedSlotId,
		};

		schedule.SwapRequests.Add(newSwapRequest);
		await _context.SwapRequests.AddAsync(newSwapRequest, ct);

		var resolvedImplicitSwaps = ResolveImplicitSwaps(ref schedule);
		_context.RemoveRange(resolvedImplicitSwaps);

		await _context.SaveChangesAsync(ct);

		return newSwapRequest;
	}

	public async Task<SwapRequest?> TryDeleteSwapRequestAsync(Guid swapRequestId, Guid actingStudentId, CancellationToken ct = default)
	{
		var swapRequest = await _context.SwapRequests
			.Where(sr => sr.Id == swapRequestId)
			.Where(sr => sr.RequestingStudentId == actingStudentId)
			.FirstOrDefaultAsync(ct);

		if (swapRequest is null)
		{
			return null;
		}

		await _context.SwapRequests
			.Where(sr => sr.Id == swapRequestId)
			.Where(sr => sr.RequestingStudentId == actingStudentId)
			.ExecuteDeleteAsync(ct);
		await _context.SaveChangesAsync(ct);
		return swapRequest;
	}

	public async Task<SwapRequest?> TryAcceptSwapRequestAsync(Guid swapRequestId, Guid actingStudentId, CancellationToken ct = default)
	{
		var schedule = await _context.Schedules
			.Where(s => s.SwapRequests.Select(sr => sr.Id).Contains(swapRequestId))
			.FirstOrDefaultAsync(ct);
		if (schedule is null)
		{
			return null;
		}

		var swapRequest = schedule.SwapRequests.FindById(swapRequestId);
		if (swapRequest is null)
		{
			return null;
		}

		var studentDictionary = await _context.StudentProfiles.FindByIdAsync([ actingStudentId, swapRequest.RequestingStudentId ], ct);
		var requestingStudent = studentDictionary[ swapRequest.RequestingStudentId ];
		var actingStudent = studentDictionary[ actingStudentId ];
		if (requestingStudent is null || actingStudent is null)
		{
			return null;
		}

		if (!StudentsInSameSchedule([ requestingStudent, actingStudent ], swapRequest.ScheduleId))
		{
			return null;
		}

		var swappingResult = schedule.TrySwapStudents(requestingStudent, actingStudent);
		if (!swappingResult)
		{
			return null;
		}

		await _context.SwapRequests
			.WhereId(swapRequestId)
			.ExecuteDeleteAsync(ct);

		await _context.SaveChangesAsync(ct);
		return swapRequest;
	}

	private static List<SwapRequest> ResolveImplicitSwaps(ref Schedule schedule)
	{
		var swapRequestAndOriginSlot = schedule.ExamSlots
			.Where(e => !e.IsLocked)
			.SelectMany(e =>
				e.Participants.Select(p => new
				{
					SlotId = e.Id,
					Participant = p
				})
			)
			.Join(schedule.SwapRequests,
				g => g.Participant.Id,
				sr => sr.RequestingStudentId,
				(g, sr) => new
				{
					g.SlotId,
					SwapRequest = sr
				})
			.ToDictionary(x => x.SlotId, x => x.SwapRequest);

		var result = new List<SwapRequest>();
		foreach (var (slotId, swapRequest) in swapRequestAndOriginSlot)
		{
			if (!result.Contains(swapRequest) && swapRequestAndOriginSlot.TryGetValue(slotId, out var implicitSwapRequest))
			{
				schedule.TrySwapStudents(swapRequest.RequestingStudentId, implicitSwapRequest.RequestingStudentId);
				result.AddRange(swapRequest, implicitSwapRequest);
			}
		}
		return result;
	}

	private static bool StudentsInSameSchedule(IEnumerable<StudentProfile> students, Guid scheduleId)
	{
		var userScheduleIds = students.Select(u => u.Classroom.Schedules.Select(s => s.Id) ?? [ ]);
		return userScheduleIds.All(ids => ids.Contains(scheduleId));
	}

	private async Task<IEnumerable<StudentProfile>?> DoAllStudentsExistAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
	{
		var students = await _context.StudentProfiles
			.WhereIds(ids)
			.DistinctBy(sp => sp.Id)
			.ToListAsync(ct);
		return students.Count == ids.Count() ? students : null;
	}
}
