using Entities;
using examScheduler.Data;
using Microsoft.EntityFrameworkCore;

namespace examScheduler.Services;

public interface IClassroomService
{
	Task<Classroom?> CreateClassroom(School school, Models.DigitalesRegister.Calendar calendar, CancellationToken ct);
	Task<Classroom?> GetClassroomAsync(School school, StudentProfile student, CancellationToken ct);
	Task<IEnumerable<Classroom>?> GetClassroomsAsync(School school, Teacher teacher, CancellationToken ct);
}

public class ClassroomService(AppDbContext context) : IClassroomService
{
	private readonly AppDbContext _context = context;

	public Task<Classroom?> CreateClassroom(School school, Models.DigitalesRegister.Calendar calendar, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public async Task<Classroom?> GetClassroomAsync(School school, StudentProfile student, CancellationToken ct = default)
	{
		return await _context.Classrooms
			.Include(c => c.Calendars)
			//better to not include in this function since the data might not be useful to every consumer
			//	.ThenInclude(c => c.Days)
			//	.ThenInclude(d => d.Lessons)
			//	.ThenInclude(l => l.Subject)
			//.Include(c => c.Calendars).ThenInclude(c => c.Days).ThenInclude(d => d.Lessons)
			//	.ThenInclude(l => l.Teachers)
			.Include(c => c.Students)
			.Include(c => c.Teachers)
			.Include(c => c.School)
			.Include(c => c.Schedules)
			.FirstOrDefaultAsync(c
			=> c.SchoolId == school.Id
			&& c.Students.Contains(student), ct);
	}

	public async Task<IEnumerable<Classroom>?> GetClassroomsAsync(School school, Teacher teacher, CancellationToken ct = default)
	{
		return await _context.Classrooms
			.Include(c => c.Calendars)
			.Include(c => c.Students)
			.Include(c => c.Teachers)
			.Include(c => c.School)
			.Include(c => c.Schedules)
			.Where(c
			=> c.SchoolId == school.Id
			&& c.Teachers.Contains(teacher))
			.ToListAsync(ct);
	}
}
