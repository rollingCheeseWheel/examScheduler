using examScheduler.Data;
using Microsoft.EntityFrameworkCore;
using Models.API;

namespace examScheduler.Services;

public interface IScheduleService
{
	Task<IEnumerable<Guid>> GetScheduleIdsAsync(Guid userId, CancellationToken ct = default);
	Task<Guid?> CreateSwapRequestAsync(Guid scheduleId, Guid userId, CancellationToken ct = default);
	Task<bool> AcceptSwapRequestAsync(Guid swapRequestId, CancellationToken ct = default);
	Task<IEnumerable<Entities.SwapRequest>> GetSwapRequestForStudentAsync(Guid userId, CancellationToken ct = default);
}

public class ScheduleService(
	AppDbContext context
) : IScheduleService
{
	private readonly AppDbContext _context = context;

	public async Task<IEnumerable<Guid>> GetScheduleIdsAsync(Guid userId, CancellationToken ct = default)
	{
		return await _context.StudentProfiles
			.Where(sp => sp.Id == userId)
			.SelectMany(sp => sp.Classroom.Schedules)
			.Select(sp => sp.Id)
			.ToListAsync(ct);
	}

	public async Task<Guid?> CreateSwapRequestAsync(Guid scheduleId, Guid userId, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public async Task<bool> AcceptSwapRequestAsync(Guid swapRequestId, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public async Task<IEnumerable<Entities.SwapRequest>> GetSwapRequestForStudentAsync(Guid userId, CancellationToken ct = default)
	{
		return await _context.SwapRequests
			.Where(sr => sr.RequestedStudentId == userId)
			.ToListAsync(ct);
	}
}
