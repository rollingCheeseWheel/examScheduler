using Entities;
using examScheduler.Data;
using Microsoft.EntityFrameworkCore;
using Models.DigitalesRegister;

namespace examScheduler.Services;

public interface IClassroomService
{
	Task<Classroom?> GetOrCreateClassroomAsync(School school, RegisterUserProfile userProfile, CancellationToken ct = default);
	Task<Classroom?> GetClassroomByRegisterIdAsync(School school, int registerId, CancellationToken ct = default);
	Task<IEnumerable<Classroom>> GetClassroomsForTeacherAsync(School school, Entities.Teacher teacher, CancellationToken ct = default);
	Task<IEnumerable<Classroom>> GetClassroomsForUserAsync(Guid userId, CancellationToken ct = default);
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
			SchoolId = school.Id,
		};
		//var newCalendar = new Calendar { Classroom = newClassroom };
		var newCalendar = new Calendar();
		newClassroom.Calendar = newCalendar;

		_context.Classrooms.Add(newClassroom);
		_context.Calendars.Add(newCalendar);
		return newClassroom;
	}

	public async Task<Classroom?> GetClassroomByRegisterIdAsync(School school, int registerId, CancellationToken ct = default) => await _context.Classrooms
			.AsNoTracking()
			.FirstOrDefaultAsync(c =>
				c.SchoolId == school.Id &&
				c.RegisterId.Contains(registerId), ct
			);

	public async Task<IEnumerable<Classroom>> GetClassroomsForTeacherAsync(School school, Entities.Teacher teacher, CancellationToken ct = default) => await _context.Classrooms
			.AsNoTracking()
			.Where(c =>
				c.SchoolId == school.Id &&
				c.Teachers.Contains(teacher))
			.ToListAsync(ct);

	public async Task<IEnumerable<Classroom>> GetClassroomsForUserAsync(Guid userId, CancellationToken ct = default)
	{
		var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
		var result = new List<Classroom>();
		if (user?.TeacherProfile is not null)
		{
			result.AddRange(user.TeacherProfile.Classrooms);
		}
		else if (user?.StudentProfile is not null)
		{
			result.Add(user.StudentProfile.Classroom);
		}
		return result;
	}
}
