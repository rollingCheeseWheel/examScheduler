using Entities;
using examScheduler.Data;
using Microsoft.EntityFrameworkCore;

namespace examScheduler.Services;

public interface IScheduleService
{
	Task<Schedule?> GetScheduleAsync(Guid? id, CancellationToken ct = default);
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
		if (id is null) return null;
		return await _context.Classrooms
			.SelectMany(c => c.Schedules)
			.FirstOrDefaultAsync(s => s.Id == id, ct);
	}

	public async Task<IEnumerable<Guid>> GetScheduleIdsForStudentAsync(Guid userId, CancellationToken ct = default)
	{
		return await _context.StudentProfiles
			.Where(sp => sp.Id == userId)
			.SelectMany(sp => sp.Classroom.Schedules)
			.Select(sp => sp.Id)
			.ToListAsync(ct);
	}

	public async Task<bool> TryEnlistStudentAsync(Guid scheduleId, Guid slotId, Guid studentId, CancellationToken ct = default)
	{
		using var transaction = await _context.Database.BeginTransactionAsync(ct);
		var student = await _studentService.GetStudentProfileAsync(studentId, ct);
		if (student is null) return false;
		var schedule = await GetScheduleAsync(scheduleId, ct);
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
		using var transaction = await _context.Database.BeginTransactionAsync(ct);

		var scheduleExists = await _context.Classrooms
			.SelectMany(c => c.Schedules)
			.Where(s => s.Id == scheduleId)
			.AnyAsync(ct);
		if (!scheduleExists)
		{
			return null;
		}

		var hasExistingSwapRequests = await _context.SwapRequests
			.Where(sr => sr.ScheduleId == scheduleId)
			.Where(sr => sr.RequestingStudentId == requestingStudentId || sr.RequestedStudentId == requestedStudentId)
			.Where(sr => sr.ExpirationDate >= DateTimeOffset.UtcNow)
			.AnyAsync(ct);
		if (hasExistingSwapRequests)
		{
			return null;
		}

		var requestingStudent = await _context.StudentProfiles
			.Select(sp => sp.UserProfile)
			.FirstOrDefaultAsync(u => u.Id == requestingStudentId, ct);
		if (requestingStudent is null)
		{
			return null;
		}

		var requestedStudentExists = await _context.StudentProfiles
			.AnyAsync(sp => sp.Id == requestedStudentId, ct);
		if (!requestedStudentExists)
		{
			return null;
		}

		var newSwapRequest = new SwapRequest
		{
			ScheduleId = scheduleId,
			RequestingStudentId = requestingStudentId,
			RequestedStudentId = requestedStudentId,
			RequestingStudentName = requestingStudent.Name,
			ExpirationDate = expirationDate
		};

		await _context.SwapRequests.AddAsync(newSwapRequest, ct);
		await _context.SaveChangesAsync(ct);
		await transaction.CommitAsync(ct);

		return newSwapRequest;
	}

	public async Task<SwapRequest?> TryDeleteSwapRequestAsync(Guid swapRequestId, Guid actingStudentId, CancellationToken ct = default)
	{
		var swapRequest = await _context.SwapRequests
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
		using var transaction = await _context.Database.BeginTransactionAsync(ct);
		var swapRequest = await _context.SwapRequests.FirstOrDefaultAsync(sr => sr.Id == swapRequestId, ct);
		if (swapRequest is null)
		{
			return null;
		}

		var existingUsers = await UsersExistsAsync(ct, swapRequest.RequestingStudentId, swapRequest.RequestedStudentId);
		if (existingUsers is null)
		{
			return null;
		}

		var requestingStudent = existingUsers.FirstOrDefault(u => u.Id == swapRequest.RequestingStudentId)?.StudentProfile;
		var requestedStudent = existingUsers.FirstOrDefault(u => u.Id == swapRequest.RequestedStudentId)?.StudentProfile;
		if (requestingStudent is null || requestedStudent is null)
		{
			return null;
		}

		if (!StudentsInSameSchedule(existingUsers, swapRequest.ScheduleId))
		{
			return null;
		}

		var schedule = await _context.Classrooms
			.Include(c => c.Schedules)
			.ThenInclude(s => s.ExamSlots)
			.ThenInclude(s => s.Participants)
			.SelectMany(c => c.Schedules)
			.FirstOrDefaultAsync(s => s.Id.Equals(swapRequest.ScheduleId), ct);
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

	private async Task<IEnumerable<UserProfile>?> UsersExistsAsync(CancellationToken ct, params Guid[ ] userIds)
	{
		var users = await _context.Users
			.Where(u => userIds.Contains(u.Id))
			.ToListAsync(ct);

		return users.Count == userIds.Length ? users : null;
	}

	private static bool StudentsInSameSchedule(IEnumerable<UserProfile> users, Guid scheduleId)
	{

		var userScheduleIds = users.Select(u => u.StudentProfile?.Classroom.Schedules.Select(s => s.Id) ?? [ ]);
		if (userScheduleIds.All(ids => ids.Contains(scheduleId)))
		{
			return true;
		}
		return false;
	}

	private async Task<int> DeleteStaleAndMatchingSwapRequestsAsync(Guid swapRequestId, CancellationToken ct = default)
	{
		return await _context.SwapRequests
			.Where(sr => sr.Id == swapRequestId || sr.ExpirationDate <= DateTimeOffset.UtcNow)
			.ExecuteDeleteAsync(ct);
	}

	private async Task<int> DeleteStaleRequestAsync(CancellationToken ct = default)
	{
		return await _context.SwapRequests
			.Where(sr => sr.ExpirationDate <= DateTimeOffset.UtcNow)
			.ExecuteDeleteAsync(ct);
	}
}
