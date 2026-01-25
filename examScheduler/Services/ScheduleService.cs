using Entities;
using examScheduler.Data;
using Microsoft.EntityFrameworkCore;
using Util.Extensions;

namespace examScheduler.Services;

public interface IScheduleService
{
	Task<Schedule?> GetScheduleAsync(Guid id, CancellationToken ct = default);
	Task<Schedule?> GetScheduleForExamSlotAsync(Guid slotId, CancellationToken ct = default);
	Task<ExamSlot?> GetExamSlotAsync(Guid id, CancellationToken ct = default);
	Task<IEnumerable<Schedule>> GetSchedulesForStudentAsync(Guid userId, CancellationToken ct = default);
	Task<IEnumerable<Guid>> GetScheduleIdsForStudentAsync(Guid userId, CancellationToken ct = default);

	Task<Guid?> TryCreateSchedule(Models.API.ScheduleCreateRequest request, Guid teacherId, CancellationToken ct = default);
	Task<Guid?> TryReportActualStudentsForScheduleSlot(Guid scheduleSlotId, IEnumerable<Models.API.UserProfile> actualParticipants, CancellationToken ct = default);

	Task<Guid?> TryEnlistStudentAsync(Guid slotId, Guid actingStudentId, CancellationToken ct = default);

	Task<SwapRequest?> CreateSwapRequestAsync(Guid scheduleId, Guid requestingStudentId, Guid requestedStudentId, DateTimeOffset expirationDate, CancellationToken ct = default);
	Task<SwapRequest?> TryDeleteSwapRequestAsync(Guid swapRequestId, Guid actingStudentId, CancellationToken ct = default);
	Task<SwapRequest?> TryAcceptSwapRequestAsync(Guid swapRequestId, Guid actingStudentId, CancellationToken ct = default);

	Task<IEnumerable<SwapRequest>> GetSwapRequestTargetingStudentAsync(Guid userId, CancellationToken ct = default);
	Task<IEnumerable<SwapRequest>> GetSwapRequestOriginatingStudentAsync(Guid userId, CancellationToken ct = default);
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
			.FirstOrDefaultAsync(e => e.Id == slotId, ct);
		return slot is null ? null : await GetScheduleAsync(slot.ScheduleId, ct);
	}

	public async Task<ExamSlot?> GetExamSlotAsync(Guid id, CancellationToken ct = default) => await _context.Schedules
			.SelectMany(s => s.ExamSlots)
			.FirstOrDefaultAsync(e => e.Id == id, ct);

	public async Task<IEnumerable<Schedule>> GetSchedulesForStudentAsync(Guid userId, CancellationToken ct = default) => await _context.StudentProfiles
			.AsNoTracking()
			.Where(sp => sp.Id == userId)
			.SelectMany(sp => sp.Classroom.Schedules)
			.ToListAsync(ct);

	public async Task<IEnumerable<Guid>> GetScheduleIdsForStudentAsync(Guid userId, CancellationToken ct = default) => await _context.StudentProfiles
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

		var teacherProfile = await _context.TeacherProfiles.FindAsync([ teacherId ], ct);
		if (teacherProfile is null) { return null; }

		var classroom = await _context.Classrooms.FindAsync([ request.ClassroomId ], ct);
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

		slot.ActuallyParticipated.AddRange(students);

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

	public async Task<SwapRequest?> CreateSwapRequestAsync(Guid scheduleId, Guid requestingStudentId, Guid requestedStudentId, DateTimeOffset expirationDate, CancellationToken ct = default)
	{
		var schedule = await _context.Schedules.FindAsync([ scheduleId ], ct);
		if (schedule is null)
		{
			return null;
		}

		var hasExistingSwapRequests = await _context.SwapRequests
			.Where(sr => sr.ScheduleId == scheduleId)
			.Where(sr => sr.ExpirationDate >= DateTimeOffset.UtcNow)
			.Where(sr => sr.RequestingStudentId == requestingStudentId || sr.RequestedStudentId == requestedStudentId)
			.AnyAsync(ct);
		if (hasExistingSwapRequests)
		{
			return null;
		}

		var requestingStudent = await _context.Users.FindAsync([ requestingStudentId ], ct);
		var requestedStudent = await _context.Users.FindAsync([ requestedStudentId ], ct);
		if (requestingStudent is null || requestedStudent is null)
		{
			return null;
		}

		var newSwapRequest = new SwapRequest
		{
			ScheduleId = scheduleId,
			RequestingStudentName = requestingStudent.Name,
			RequestedStudentName = requestedStudent.Name,
			RequestingStudentId = requestingStudentId,
			RequestedStudentId = requestedStudentId,
			ExpirationDate = expirationDate
		};

		schedule.SwapRequests.Add(newSwapRequest);
		await _context.SwapRequests.AddAsync(newSwapRequest, ct);
		await _context.SaveChangesAsync(ct);

		return newSwapRequest;
	}

	public async Task<SwapRequest?> TryDeleteSwapRequestAsync(Guid swapRequestId, Guid actingStudentId, CancellationToken ct = default)
	{
		var swapRequest = await _context.SwapRequests
			.Where(sr => sr.Id == swapRequestId)
			.Where(sr => sr.RequestingStudentId == actingStudentId
				|| sr.RequestedStudentId == actingStudentId)
			.FirstOrDefaultAsync(ct);

		if (swapRequest is null)
		{
			return null;
		}

		await _context.SwapRequests
			.Where(sr => sr.Id == swapRequestId)
			.Where(sr => sr.RequestedStudentId == actingStudentId
				|| sr.RequestingStudentId == actingStudentId)
			.ExecuteDeleteAsync(ct);
		await _context.SaveChangesAsync(ct);
		return swapRequest;
	}

	public async Task<SwapRequest?> TryAcceptSwapRequestAsync(Guid swapRequestId, Guid actingStudentId, CancellationToken ct = default)
	{
		var swapRequest = await _context.SwapRequests.FindAsync([ swapRequestId ], ct);
		if (swapRequest is null)
		{
			return null;
		}

		var requestingStudentTask = _context.StudentProfiles.FindAsync([ swapRequest.RequestingStudentId ], ct).AsTask();
		var requestedStudentTask = _context.StudentProfiles.FindAsync([ swapRequest.RequestedStudentId ], ct).AsTask();
		await Task.WhenAll(requestingStudentTask, requestedStudentTask).WaitAsync(ct);


		var requestingStudent = requestingStudentTask.Result;
		var requestedStudent = requestedStudentTask.Result;
		if (requestingStudent is null || requestedStudent is null)
		{
			return null;
		}

		if (!StudentsInSameSchedule([ requestingStudent, ], swapRequest.ScheduleId))
		{
			return null;
		}

		var schedule = await _context.Schedules.FindAsync([ swapRequest.ScheduleId ], ct);
		if (schedule is null)
		{
			return null;
		}

		var swappingResult = schedule.TrySwapStudents(requestingStudent, requestedStudent);
		if (!swappingResult)
		{
			return null;
		}

		await DeleteStaleAndMatchingSwapRequestsAsync(swapRequestId, ct);

		await _context.SaveChangesAsync(ct);
		return swapRequest;
	}

	public async Task<IEnumerable<SwapRequest>> GetSwapRequestTargetingStudentAsync(Guid userId, CancellationToken ct = default) => await _context.SwapRequests
			.Where(sr => sr.RequestedStudentId == userId)
			.ToListAsync(ct);

	public async Task<IEnumerable<SwapRequest>> GetSwapRequestOriginatingStudentAsync(Guid userId, CancellationToken ct = default) => await _context.SwapRequests
			.Where(sr => sr.RequestingStudentId == userId)
			.ToListAsync(ct);

	private static bool StudentsInSameSchedule(IEnumerable<StudentProfile> students, Guid scheduleId)
	{
		var userScheduleIds = students.Select(u => u.Classroom.Schedules.Select(s => s.Id) ?? [ ]);
		return userScheduleIds.All(ids => ids.Contains(scheduleId));
	}

	private async Task<int> DeleteStaleAndMatchingSwapRequestsAsync(Guid swapRequestId, CancellationToken ct = default) => await _context.SwapRequests
			.Where(sr => sr.Id == swapRequestId || sr.ExpirationDate <= DateTimeOffset.UtcNow)
			.ExecuteDeleteAsync(ct);

	private async Task<IEnumerable<StudentProfile>?> DoAllStudentsExistAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
	{
		var students = await _context.StudentProfiles
			.Where(sp => ids.Contains(sp.Id))
			.DistinctBy(sp => sp.Id)
			.ToListAsync(ct);
		return students.Count == ids.Count() ? students : null;
	}
}
