using Entities;
using examScheduler.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using Util;

namespace examScheduler.Services;

public interface IScheduleService
{
	Task<Schedule?> GetScheduleAsync(Guid? id, CancellationToken ct = default);
	Task<Schedule?> GetScheduleForExamSlotAsync(Guid slotId, CancellationToken ct = default);
	Task<ExamSlot?> GetExamSlotAsync(Guid id, CancellationToken ct = default);
	Task<IEnumerable<Schedule>> GetSchedulesForStudentAsync(Guid userId, CancellationToken ct = default);
	Task<IEnumerable<Guid>> GetScheduleIdsForStudentAsync(Guid userId, CancellationToken ct = default);

	Task<Guid?> TryCreateSchedule(Models.API.ScheduleCreateRequest request, CancellationToken ct = default);
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

	public async Task<Schedule?> GetScheduleAsync(Guid? id, CancellationToken ct = default) => id is null
			? null
			: await _context.Schedules.FindAsync([ id ], ct);

	public async Task<Schedule?> GetScheduleForExamSlotAsync(Guid slotId, CancellationToken ct = default)
	{
		var slot = await _context.Schedules
			.SelectMany(s => s.ExamSlots)
			.FirstOrDefaultAsync(e => e.Id == slotId, ct);
		if (slot is null)
		{
			return null;
		}
		return await GetScheduleAsync(slot.ScheduleId, ct);
	}

	public async Task<ExamSlot?> GetExamSlotAsync(Guid id, CancellationToken ct = default)
	{
		return await _context.Schedules
			.SelectMany(s => s.ExamSlots)
			.FirstOrDefaultAsync(e => e.Id == id, ct);
	}

	public async Task<IEnumerable<Schedule>> GetSchedulesForStudentAsync(Guid userId, CancellationToken ct = default) => await _context.StudentProfiles
			.Where(sp => sp.Id == userId)
			.SelectMany(sp => sp.Classroom.Schedules)
			.ToListAsync(ct);

	public async Task<IEnumerable<Guid>> GetScheduleIdsForStudentAsync(Guid userId, CancellationToken ct = default) => await _context.StudentProfiles
			.Where(sp => sp.Id == userId)
			.SelectMany(sp => sp.Classroom.Schedules)
			.Select(s => s.Id)
			.ToListAsync(ct);

	public async Task<Guid?> TryCreateSchedule(Models.API.ScheduleCreateRequest request, CancellationToken ct = default)
	{
		using var transaction = await _context.Database.BeginTransactionAsync(ct);


		throw new NotImplementedException();
	}

	public async Task<Guid?> TryReportActualStudentsForScheduleSlot(Guid scheduleSlotId, IEnumerable<Models.API.UserProfile> participants, CancellationToken ct = default)
	{
		using var transaction = await _context.Database.BeginTransactionAsync(ct);

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
		await transaction.CommitAsync(ct);
		return slot.Id;
	}

	public async Task<Guid?> TryEnlistStudentAsync(Guid slotId, Guid studentId, CancellationToken ct = default)
	{
		using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync(ct);
		StudentProfile? student = await _studentService.GetStudentProfileAsync(studentId, ct);
		if (student is null)
		{
			return null;
		}

		Schedule? schedule = await GetScheduleForExamSlotAsync(slotId, ct);
		if (schedule is null)
		{
			return null;
		}

		if (schedule.TryEnlistStudent(slotId, student))
		{
			await _context.SaveChangesAsync(ct);
			await transaction.CommitAsync(ct);
			return schedule.Id;
		}
		return null;
	}

	public async Task<SwapRequest?> CreateSwapRequestAsync(Guid scheduleId, Guid requestingStudentId, Guid requestedStudentId, DateTimeOffset expirationDate, CancellationToken ct = default)
	{
		using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync(ct);

		Schedule? schedule = await _context.Schedules.FindAsync([ scheduleId ], ct);
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

		UserProfile? requestingStudent = await _context.Users.FindAsync([ requestingStudentId ], ct);
		UserProfile? requestedStudent = await _context.Users.FindAsync([ requestedStudentId ], ct);
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
		await transaction.CommitAsync(ct);

		return newSwapRequest;
	}

	public async Task<SwapRequest?> TryDeleteSwapRequestAsync(Guid swapRequestId, Guid actingStudentId, CancellationToken ct = default)
	{
		SwapRequest? swapRequest = await _context.SwapRequests
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
		using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync(ct);
		SwapRequest? swapRequest = await _context.SwapRequests.FindAsync([ swapRequestId ], ct);
		if (swapRequest is null)
		{
			return null;
		}

		Task<StudentProfile?> requestingStudentTask = _context.StudentProfiles.FindAsync([ swapRequest.RequestingStudentId ], ct).AsTask();
		Task<StudentProfile?> requestedStudentTask = _context.StudentProfiles.FindAsync([ swapRequest.RequestedStudentId ], ct).AsTask();
		await Task.WhenAll(requestingStudentTask, requestedStudentTask).WaitAsync(ct);


		StudentProfile? requestingStudent = requestingStudentTask.Result;
		StudentProfile? requestedStudent = requestedStudentTask.Result;
		if (requestingStudent is null || requestedStudent is null)
		{
			return null;
		}

		if (!StudentsInSameSchedule([ requestingStudent, ], swapRequest.ScheduleId))
		{
			return null;
		}

		Schedule? schedule = await _context.Schedules.FindAsync([ swapRequest.ScheduleId ], ct);
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
		await transaction.CommitAsync(ct);
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
		IEnumerable<IEnumerable<Guid>> userScheduleIds = students.Select(u => u.Classroom.Schedules.Select(s => s.Id) ?? [ ]);
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

	private static bool StudentsInSameClassroom(IEnumerable<StudentProfile> students)
	{
		var classroomId = students.FirstOrDefault()?.ClassroomId;
		return classroomId is not null && students.All(s => s.ClassroomId == classroomId);
	}
}
