using Entities;
using examScheduler.Data;
using Microsoft.EntityFrameworkCore;

namespace examScheduler.Services;

public interface IClassroomService
{
	Task<Classroom?> GetClassroomAsync(School school, UserProfile userProfile, CancellationToken ct);

	Task<Classroom?> CreateClassroom(School school, Models.DigitalesRegister.Calendar calendar, CancellationToken ct);
}

public class ClassroomService(AppDbContext context) : IClassroomService
{
	private readonly AppDbContext _context = context;

	public Task<Classroom?> GetClassroomAsync(School school, UserProfile userProfile, CancellationToken ct)
	{
		throw new NotImplementedException();
	}

	public Task<Classroom?> CreateClassroom(School school, Models.DigitalesRegister.Calendar calendar, CancellationToken ct)
	{
		throw new NotImplementedException();
	}
}
