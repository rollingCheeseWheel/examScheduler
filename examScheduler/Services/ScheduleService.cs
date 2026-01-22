using Entities;
using examScheduler.Data;
using Microsoft.EntityFrameworkCore;

namespace examScheduler.Services;

public interface IScheduleService
{
	Task<Schedule?> GetScheduleAsync(Guid? id, CancellationToken ct = default);
	Task<IEnumerable<Schedule>> GetSchedulesForStudentAsync(Guid userId, CancellationToken ct = default);
	Task<IEnumerable<Guid>> GetScheduleIdsForStudentAsync(Guid userId, CancellationToken ct = default);

	Task<bool> TryEnlistStudentAsync(Guid scheduleId, Guid slotId, Guid actingStudentId, CancellationToken ct = default);

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

	public async Task<Schedule?> GetScheduleAsync(Guid? id, CancellationToken ct = default)
	{
		return id is null
			? null
			: await _context.Schedules.FindAsync([ id ], ct);
	}

	public async Task<IEnumerable<Schedule>> GetSchedulesForStudentAsync(Guid userId, CancellationToken ct = default)
	{
		return await _context.StudentProfiles
			.Where(sp => sp.Id == userId)
			.SelectMany(sp => sp.Classroom.Schedules)
			.ToListAsync(ct);
	}

	public async Task<IEnumerable<Guid>> GetScheduleIdsForStudentAsync(Guid userId, CancellationToken ct = default)
	{
		return await _context.StudentProfiles
			.Where(sp => sp.Id == userId)
			.SelectMany(sp => sp.Classroom.Schedules)
			.Select(s => s.Id)
			.ToListAsync(ct);
	}

	public async Task<bool> TryEnlistStudentAsync(Guid scheduleId, Guid slotId, Guid studentId, CancellationToken ct = default)
	{
		using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync(ct);
		StudentProfile? student = await _studentService.GetStudentProfileAsync(studentId, ct);
		if (student is null) return false;
		Schedule? schedule = await GetScheduleAsync(scheduleId, ct);
		if (schedule is null) return false;

		if (schedule.TryEnlistStudent(slotId, student))
		{
			await _context.SaveChangesAsync(ct);
			await transaction.CommitAsync(ct);
			return true;
		}
		return false;
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

		if (swapRequest is null) return null;

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

		var requestingStudentTask = _context.StudentProfiles.FindAsync([ swapRequest.RequestingStudentId ], ct).AsTask();
		var requestedStudentTask = _context.StudentProfiles.FindAsync([ swapRequest.RequestedStudentId ], ct).AsTask();
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
		if (!swappingResult) return null;
		await DeleteStaleAndMatchingSwapRequestsAsync(swapRequestId, ct);

		await _context.SaveChangesAsync(ct);
		await transaction.CommitAsync(ct);
		return swapRequest;
	}

	public async Task<IEnumerable<SwapRequest>> GetSwapRequestTargetingStudentAsync(Guid userId, CancellationToken ct = default)
	{
		return await _context.SwapRequests
			.Where(sr => sr.RequestedStudentId == userId)
			.ToListAsync(ct);
	}

	public async Task<IEnumerable<SwapRequest>> GetSwapRequestOriginatingStudentAsync(Guid userId, CancellationToken ct = default)
	{
		return await _context.SwapRequests
			.Where(sr => sr.RequestingStudentId == userId)
			.ToListAsync(ct);
	}

	private static bool StudentsInSameSchedule(IEnumerable<StudentProfile> users, Guid scheduleId)
	{

		IEnumerable<IEnumerable<Guid>> userScheduleIds = users.Select(u => u.Classroom.Schedules.Select(s => s.Id) ?? [ ]);
		return userScheduleIds.All(ids => ids.Contains(scheduleId));
	}

	private async Task<int> DeleteStaleAndMatchingSwapRequestsAsync(Guid swapRequestId, CancellationToken ct = default)
	{
		return await _context.SwapRequests
			.Where(sr => sr.Id == swapRequestId || sr.ExpirationDate <= DateTimeOffset.UtcNow)
			.ExecuteDeleteAsync(ct);
	}
}
