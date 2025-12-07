using Entities;
using examScheduler.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Models.DigitalesRegister;

namespace examScheduler.Services;

public interface IClassroomService
{
	Task<Classroom?> GetOrCreateClassroomAsync(School school, RegisterUserProfile userProfile, CancellationToken ct = default);
	Task<Classroom?> GetClassroomByRegisterIdAsync(School school, int registerId, CancellationToken ct = default);
	Task<IEnumerable<Classroom>> GetClassroomsAsync(School school, Entities.Teacher teacher, CancellationToken ct = default);
}

public class ClassroomService(AppDbContext context) : IClassroomService
{
	private readonly AppDbContext _context = context;

	public async Task<Classroom?> GetOrCreateClassroomAsync(School school, RegisterUserProfile userProfile, CancellationToken ct = default)
	{
		if (userProfile.StudentData is null ||
			userProfile.StudentData.MainClass is null) { return null; }

		var existingClassroom = await GetClassroomByRegisterIdAsync(school, userProfile.StudentData.MainClass.Id, ct);
		if (existingClassroom is not null) { return existingClassroom; }

		var newClassroom = new Classroom
		{
			Name = userProfile.StudentData.MainClass.Name,
			RegisterId = [ userProfile.StudentData.MainClass.Id ],
			School = school,
		};
		var newCalendar = new Calendar { Classroom = newClassroom };
		newClassroom.Calendar = newCalendar;

		_context.Classrooms.Add(newClassroom);
		return newClassroom;
	}

	public async Task<Classroom?> GetClassroomByRegisterIdAsync(School school, int registerId, CancellationToken ct = default)
	{
		return await GetClassrooms()
			.FirstOrDefaultAsync(c
			=> c.SchoolId == school.Id
			&& c.RegisterId.Contains(registerId), ct);
	}

	public async Task<IEnumerable<Classroom>> GetClassroomsAsync(School school, Entities.Teacher teacher, CancellationToken ct = default)
	{
		return await GetClassrooms()
			.Where(c
			=> c.SchoolId == school.Id
			&& c.Teachers.Contains(teacher))
			.ToListAsync(ct);
	}

	private IQueryable<Classroom> GetClassrooms()
	{
		return _context.Classrooms
			.Include(c => c.Calendar)
				.ThenInclude(cal => cal != null ? cal.Lessons : Enumerable.Empty<Entities.Lesson>())
				.ThenInclude(l => l.Subject)
			.Include(c => c.Calendar)
				.ThenInclude(cal => cal != null ? cal.Lessons : Enumerable.Empty<Entities.Lesson>())
				.ThenInclude(l => l.Teachers)
				.ThenInclude(t => t.Subjects)
			.Include(c => c.Calendar)
				.ThenInclude(cal => cal != null ? cal.Lessons : Enumerable.Empty<Entities.Lesson>())
				.ThenInclude(l => l.Occurances)
			.Include(c => c.Students)
			.Include(c => c.Teachers)
			.Include(c => c.School)
			.Include(c => c.Schedules);
	}
}
