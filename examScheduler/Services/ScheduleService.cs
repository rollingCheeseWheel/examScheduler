using Entities;
using examScheduler.Data;
using examScheduler.Mappings;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;
using Util.Extensions;

namespace examScheduler.Services;

public interface IScheduleService
{
	Task<Schedule?> GetScheduleAsync(Guid id, CancellationToken ct = default);
	Task<IEnumerable<Schedule>> GetSchedulesForStudentAsync_AsNoTracking(Guid userId, CancellationToken ct = default);
	Task<IEnumerable<Guid>> GetScheduleIdsForStudentAsync_AsNoTracking(Guid userId, CancellationToken ct = default);

	Task<Guid?> TryCreateSchedule(Models.API.ScheduleCreateRequest request, Guid teacherId, CancellationToken ct = default);
	Task<Guid?> TryReportActualStudentsForScheduleSlot(Guid examSlotId, Guid teacherId, IEnumerable<Models.API.UserProfile> actualParticipants, CancellationToken ct = default);

	Task<Guid?> TryEnlistStudentAsync(Guid slotId, Guid actingStudentId, CancellationToken ct = default);

	Task<bool> TryCreateSwapRequestAsync(Guid scheduleId, Guid requestingStudentId, Guid requestedSlotId, CancellationToken ct = default);
	Task<Guid?> TryDeleteSwapRequestAsync(Guid swapRequestId, Guid owningStudentId, CancellationToken ct = default);
	Task<Guid?> TryAcceptSwapRequestAsync(Guid swapRequestId, Guid acceptingStudentId, CancellationToken ct = default);
}

public class ScheduleService(
	AppDbContext context
) : IScheduleService
{
	private readonly AppDbContext _context = context;

	public async Task<Schedule?> GetScheduleAsync(Guid id, CancellationToken ct = default) => await _context.Classrooms.SelectMany(c => c.Schedules).FindByIdAsync(id, ct);

	public async Task<IEnumerable<Schedule>> GetSchedulesForStudentAsync_AsNoTracking(Guid userId, CancellationToken ct = default) => await _context.Users
			.Select(u => u.StudentProfile)
			.WhereNotNull()
			.AsNoTracking()
			.Where(sp => sp.Id == userId)
			.SelectMany(sp => sp.Classroom.Schedules)
			.ToListAsync(ct);

	public async Task<IEnumerable<Guid>> GetScheduleIdsForStudentAsync_AsNoTracking(Guid userId, CancellationToken ct = default) => await _context.Users
			.Select(u => u.StudentProfile)
			.WhereNotNull()
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

		var teacherProfile = await _context.Users
			.Select(u => u.TeacherProfile)
			.WhereNotNull()
			.FindByIdAsync(teacherId, ct);
		if (teacherProfile is null || teacherProfile.Teacher is null) { return null; }

		var classroom = await _context.Classrooms.FindByIdAsync(request.ClassroomId, ct);
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

		var newSchedule = new Schedule
		{
			SlotFillingBehaviour = request.SlotFillingBehaviour,
			AutoLockIn = request.AutoLockIn,
			AutoLockInOffset = request.LockInOffset,
			StartDate = request.StartDate,
			EndDate = request.EndDate,
			Subject = subject,
			Teachers = [ .. classroom.Teachers.Where(t => t.Subjects.Contains(subject)) ],
			ExamSlots = [ .. request.GeneratorSlots.Select(g => new ExamSlot
			{
				Date = request.StartDate + g.Offset,
				MaxParticipants = g.MaxParticipants,
				MinParticipants = g.MinParticipants,
			}) ]
		};

		foreach (var slot in newSchedule.ExamSlots)
		{
			slot.Schedule = newSchedule;
		}

		classroom.Schedules.Add(newSchedule);
		await _context.SaveChangesAsync(ct);

		return newSchedule.Id;
	}

	public async Task<Guid?> TryReportActualStudentsForScheduleSlot(Guid examSlotId, Guid teacherId, IEnumerable<Models.API.UserProfile> participants, CancellationToken ct = default)
	{
		var schedule = await _context.Classrooms
			.SelectMany(c => c.Schedules)
			.Where(s => s.ExamSlots.ContainsId(examSlotId))
			.FirstOrDefaultAsync(ct);
		if (schedule is null)
		{
			return null;
		}

		var slot = schedule.ExamSlots.FindById(examSlotId);
		if (slot is null)
		{
			return null;
		}

		var students = await GetAllStudentsExactAsync(participants.Select(x => x.Id), ct);
		if (students is null || !students.Any())
		{
			return null;
		}

		var isSuccess = schedule.TryReportStudents(examSlotId, teacherId, students);
		if (!isSuccess)
		{
			return null;
		}

		await _context.SaveChangesAsync(ct);
		return slot.Id;
	}

	public async Task<Guid?> TryEnlistStudentAsync(Guid slotId, Guid studentId, CancellationToken ct = default)
	{
		var student = await _context.Users
			.Select(u => u.StudentProfile)
			.WhereNotNull()
			.FindByIdAsync(studentId, ct);
		if (student is null)
		{
			return null;
		}

		var schedule = await _context.Classrooms
			.SelectMany(c => c.Schedules)
			.Where(s => s.ExamSlots.ContainsId(slotId))
			.FirstOrDefaultAsync(ct);
		if (schedule is null)
		{
			return null;
		}

		var isSuccess = schedule.TryEnlistStudent(slotId, student);
		if (!isSuccess)
		{
			return null;
		}
		await _context.SaveChangesAsync(ct);
		return schedule.Id;
	}

	public async Task<bool> TryCreateSwapRequestAsync(Guid scheduleId, Guid requestingStudentId, Guid requestedSlotId, CancellationToken ct = default)
	{
		var schedule = await _context.Classrooms.SelectMany(c => c.Schedules).FindByIdAsync(scheduleId, ct);
		if (schedule is null)
		{
			return false;
		}

		var requestingStudent = await _context.Users
			.Select(u => u.StudentProfile)
			.WhereNotNull()
			.FindByIdAsync(requestingStudentId, ct);
		if (requestingStudent is null)
		{
			return false;
		}

		var newSwapRequest = new SwapRequest
		{
			ScheduleId = scheduleId,
			RequestingStudentName = requestingStudent.UserProfile.Name,
			RequestingStudentId = requestingStudentId,
			RequestedSlotId = requestedSlotId,
		};

		var isSuccess = schedule.TryAddSwapRequest(newSwapRequest);
		schedule.ResolveImplicitSwaps();

		await _context.SaveChangesAsync(ct);

		return true;
	}

	public async Task<Guid?> TryDeleteSwapRequestAsync(Guid swapRequestId, Guid actingStudentId, CancellationToken ct = default)
	{
		var schedule = await _context.Classrooms
			.SelectMany(c => c.Schedules)
			.Where(s => s.SwapRequests.ContainsId(swapRequestId))
			.FirstOrDefaultAsync(ct);

		if (schedule is null)
		{
			return null;
		}

		var isSuccess = schedule.TryDeleteSwapRequest(swapRequestId);
		if (!isSuccess)
		{
			return null;
		}

		await _context.SaveChangesAsync(ct);
		return schedule.Id;
	}

	public async Task<Guid?> TryAcceptSwapRequestAsync(Guid swapRequestId, Guid actingStudentId, CancellationToken ct = default)
	{
		var schedule = await _context.Classrooms
			.SelectMany(c => c.Schedules)
			.Where(s => s.SwapRequests.ContainsId(swapRequestId))
			.FirstOrDefaultAsync(ct);
		if (schedule is null)
		{
			return null;
		}

		var swappingResult = schedule.TryAcceptSwapRequest(swapRequestId, actingStudentId);
		if (!swappingResult)
		{
			return null;
		}

		await _context.SaveChangesAsync(ct);
		return schedule.Id;
	}

	private async Task<IEnumerable<StudentProfile>?> GetAllStudentsExactAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
	{
		var students = await _context.Users
			.Select(u => u.StudentProfile)
			.WhereNotNull()
			.WhereIds(ids)
			.ToListAsync(ct);
		return students.Count == ids.Count() ? students : null;
	}
}
