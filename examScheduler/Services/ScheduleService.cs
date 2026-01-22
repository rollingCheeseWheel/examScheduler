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
			: await _context.Classrooms
			.SelectMany(c => c.Schedules)
			.FirstOrDefaultAsync(s => s.Id == id, ct);
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

		Schedule? schedule = await _context.Classrooms
			.SelectMany(c => c.Schedules)
			.FirstOrDefaultAsync(s => s.Id == scheduleId);
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

		UserProfile? requestingStudent = await _context.Users
			.FirstOrDefaultAsync(u => u.Id == requestingStudentId, ct);
		var requestedStudent = await _context.Users.FirstOrDefaultAsync(u => u.Id == requestedStudentId, ct);
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
		SwapRequest? swapRequest = await _context.SwapRequests.FirstOrDefaultAsync(sr => sr.Id == swapRequestId, ct);
		if (swapRequest is null)
		{
			return null;
		}

		IEnumerable<UserProfile>? existingUsers = await UsersExistsAsync(ct, swapRequest.RequestingStudentId, swapRequest.RequestedStudentId);
		if (existingUsers is null)
		{
			return null;
		}

		StudentProfile? requestingStudent = existingUsers.FirstOrDefault(u => u.Id == swapRequest.RequestingStudentId)?.StudentProfile;
		StudentProfile? requestedStudent = existingUsers.FirstOrDefault(u => u.Id == swapRequest.RequestedStudentId)?.StudentProfile;
		if (requestingStudent is null || requestedStudent is null)
		{
			return null;
		}

		if (!StudentsInSameSchedule(existingUsers, swapRequest.ScheduleId))
		{
			return null;
		}

		Schedule? schedule = await _context.Classrooms
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
		List<UserProfile> users = await _context.Users
			.Where(u => userIds.Contains(u.Id))
			.ToListAsync(ct);

		return users.Count == userIds.Length ? users : null;
	}

	private static bool StudentsInSameSchedule(IEnumerable<UserProfile> users, Guid scheduleId)
	{

		IEnumerable<IEnumerable<Guid>> userScheduleIds = users.Select(u => u.StudentProfile?.Classroom.Schedules.Select(s => s.Id) ?? [ ]);
		return userScheduleIds.All(ids => ids.Contains(scheduleId));
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
