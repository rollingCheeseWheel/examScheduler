using Entities;
using examScheduler.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.IdentityModel.Abstractions;
using Models.API;
using System.Runtime.InteropServices;

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
		await _context.SaveChangesAsync(ct);

		return newSwapRequest.Id;
	}

	public async Task<bool> AcceptSwapRequestAsync(Guid swapRequestId, CancellationToken ct = default)
	{
		try
		{
			var swapRequest = await _context.SwapRequests.FirstOrDefaultAsync(sr => sr.Id == swapRequestId, ct);
			if (swapRequest is null)
			{
				return false;
			}

			var existingUsers = await UsersExistsAsync(ct, swapRequest.RequestingStudentId, swapRequest.RequestedStudentId);
			if (existingUsers is null)
			{
				return false;
			}

			var requestingStudent = existingUsers.FirstOrDefault(u => u.Id == swapRequest.RequestingStudentId)?.StudentProfile;
			var requestedStudent = existingUsers.FirstOrDefault(u => u.Id == swapRequest.RequestedStudentId)?.StudentProfile;
			if (requestingStudent is null || requestedStudent is null)
			{
				return false;
			}

			if (!StudentsInSameSchedule(existingUsers, swapRequest.ScheduleId))
			{
				return false;
			}

			var schedule = await _context.Classrooms
				.Include(c => c.Schedules)
				.ThenInclude(s => s.ExamSlots)
				.ThenInclude(s => s.Participants)
				.SelectMany(c => c.Schedules)
				.FirstOrDefaultAsync(s => s.Id == swapRequest.ScheduleId, ct);
			if (schedule is null)
			{
				return false;
			}

			return schedule.TrySwapStudents(requestingStudent, requestedStudent);
		}
		finally
		{
			try
			{
				await DeleteSwapRequestAsync(swapRequestId, ct);
			}
			catch { }
			await _context.SaveChangesAsync(ct);
		}
	}

	public async Task<IEnumerable<Entities.SwapRequest>> GetSwapRequestForStudentAsync(Guid userId, CancellationToken ct = default)
	{
		return await _context.SwapRequests
			.Where(sr => sr.RequestedStudentId == userId)
			.ToListAsync(ct);
	}

	private async Task<IEnumerable<Entities.UserProfile>?> UsersExistsAsync(CancellationToken ct, params Guid[ ] userIds)
	{
		var users = await _context.Users
			.Where(u => userIds.Contains(u.Id))
			.ToListAsync(ct);

		return users.Count == userIds.Length ? users : null;
	}

	private async Task<IEnumerable<Entities.UserProfile>?> StudentsInSameScheduleAsync(CancellationToken ct, params Guid[ ] userIds)
	{
		var users = await _context.Users
			.Include(u => u.StudentProfile)
			.ThenInclude(sp => sp != null ? sp.Classroom : null)
			.ThenInclude(c => c != null ? c.Schedules : new List<Entities.Schedule>())
			.Where(u => userIds.Contains(u.Id))
			.ToListAsync(ct);
		return StudentsInSameSchedule(users) ? users : null;
	}

	private static bool StudentsInSameSchedule(IEnumerable<Entities.UserProfile> users) => StudentsInSameSchedule(users, out var _);

	private static bool StudentsInSameSchedule(IEnumerable<Entities.UserProfile> users, out Guid foundScheduleId)
	{
		foundScheduleId = Guid.Empty;
		var firstUser = users.FirstOrDefault();
		if (firstUser is null || users.Any(u => u.StudentProfile == null))
		{
			return false;
		}

		var scheduleIds = firstUser.StudentProfile?.Classroom.Schedules.Select(s => s.Id) ?? [ ];
		foreach (var scheduleId in scheduleIds)
		{
			if (StudentsInSameSchedule(users, scheduleId))
			{
				foundScheduleId = scheduleId;
				return true;
			}
		}
		return false;
	}

	private static bool StudentsInSameSchedule(IEnumerable<Entities.UserProfile> users, Guid scheduleId)
	{

		var userScheduleIds = users.Select(u => u.StudentProfile?.Classroom.Schedules.Select(s => s.Id) ?? [ ]);
		if (userScheduleIds.All(ids => ids.Contains(scheduleId)))
		{
			return true;
		}
		return false;
	}

	private async Task DeleteSwapRequestAsync(Guid swapRequestId, CancellationToken ct = default)
	{
		await _context.SwapRequests
			.Where(sr => sr.Id == swapRequestId || sr.ExpirationDate <= DateTimeOffset.UtcNow)
			.ExecuteDeleteAsync(ct);
	}
}
