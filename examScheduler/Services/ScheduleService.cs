using examScheduler.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.IdentityModel.Abstractions;
using Models.API;

namespace examScheduler.Services;

public interface IScheduleService
{
	Task<IEnumerable<Guid>> GetScheduleIdsForStudentAsync(Guid userId, CancellationToken ct = default);
	Task<Guid?> CreateSwapRequestAsync(Guid scheduleId, Guid requestingStudentId, Guid requestedStudentId, DateTimeOffset expirationDate, CancellationToken ct = default);
	Task<bool> AcceptSwapRequestAsync(Guid swapRequestId, CancellationToken ct = default);
	Task<IEnumerable<Entities.SwapRequest>> GetSwapRequestForStudentAsync(Guid userId, CancellationToken ct = default);
}

public class ScheduleService(
	AppDbContext context
) : IScheduleService
{
	private readonly AppDbContext _context = context;

	public async Task<IEnumerable<Guid>> GetScheduleIdsForStudentAsync(Guid userId, CancellationToken ct = default)
	{
		return await _context.StudentProfiles
			.Where(sp => sp.Id == userId)
			.SelectMany(sp => sp.Classroom.Schedules)
			.Select(sp => sp.Id)
			.ToListAsync(ct);
	}

	public async Task<Guid?> CreateSwapRequestAsync(Guid scheduleId, Guid requestingStudentId, Guid requestedStudentId, DateTimeOffset expirationDate, CancellationToken ct = default)
	{
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

		var newSwapRequest = new Entities.SwapRequest
		{
			ScheduleId = scheduleId,
			RequestingStudentId = requestingStudentId,
			RequestedStudentId = requestedStudentId,
			RequestingStudentName = requestingStudent.Name,
			ExpirationDate = expirationDate
		};

		await _context.SwapRequests.AddAsync(newSwapRequest, ct);

		return newSwapRequest.Id;
	}

	public async Task<bool> AcceptSwapRequestAsync(Guid swapRequestId, CancellationToken ct = default)
	{
		var swapRequest = await _context.SwapRequests.FirstOrDefaultAsync(sr => sr.Id == swapRequestId, ct);
		if (swapRequest is null)
		{
			await DeleteSwapRequest(swapRequestId, ct);
			return false;
		}

		if (!await UsersExistsAsync(ct, swapRequest.RequestingStudentId, swapRequest.RequestedStudentId))
		{
			await DeleteSwapRequest(swapRequestId, ct);
			return false;
		}

		throw new NotImplementedException();


	}

	public async Task<IEnumerable<Entities.SwapRequest>> GetSwapRequestForStudentAsync(Guid userId, CancellationToken ct = default)
	{
		return await _context.SwapRequests
			.Where(sr => sr.RequestedStudentId == userId)
			.ToListAsync(ct);
	}

	private async Task<bool> UsersExistsAsync(CancellationToken ct, params Guid[ ] userIds)
	{
		return await _context.Users.AllAsync(u => userIds.Contains(u.Id), ct);
	}

	private async Task DeleteSwapRequest(Guid swapRequestId, CancellationToken ct = default)
	{
		await _context.SwapRequests
			.Where(sr => sr.Id == swapRequestId || sr.ExpirationDate <= DateTimeOffset.UtcNow)
			.ExecuteDeleteAsync(ct);
	}
}
