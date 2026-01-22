using Entities;
using examScheduler.Data;
using Microsoft.EntityFrameworkCore;
using Models.DigitalesRegister;

namespace examScheduler.Services;

public interface IClassroomService
{
	Task<Classroom?> GetOrCreateClassroomAsync(School school, RegisterUserProfile userProfile, CancellationToken ct = default);
	Task<Classroom?> GetClassroomByRegisterIdAsync(School school, int registerId, CancellationToken ct = default);
	Task<IEnumerable<Classroom>> GetClassroomsAsync(School school, Entities.Teacher teacher, CancellationToken ct = default);
	Task<IEnumerable<Classroom>> GetClassroomsForUserAsync(Guid userId, CancellationToken ct = default);
}

public class ClassroomService(AppDbContext context) : IClassroomService
{
	private readonly AppDbContext _context = context;

	public async Task<Classroom?> GetOrCreateClassroomAsync(School school, RegisterUserProfile userProfile, CancellationToken ct = default)
	{
		if (userProfile.StudentData is null ||
			userProfile.StudentData.MainClass is null) { return null; }

		Classroom? existingClassroom = await GetClassroomByRegisterIdAsync(school, userProfile.StudentData.MainClass.Id, ct);
		if (existingClassroom is not null) { return existingClassroom; }

		var newClassroom = new Classroom
		{
			Name = userProfile.StudentData.MainClass.Name,
			RegisterId = [ userProfile.StudentData.MainClass.Id ],
			School = school,
		};
		//var newCalendar = new Calendar { Classroom = newClassroom };
		var newCalendar = new Calendar();
		newClassroom.Calendar = newCalendar;

		_context.Classrooms.Add(newClassroom);
		_context.Calendars.Add(newCalendar);
		return newClassroom;
	}

	public async Task<Classroom?> GetClassroomByRegisterIdAsync(School school, int registerId, CancellationToken ct = default)
	{
		return await _context.Classrooms
			.FirstOrDefaultAsync(c => 
				c.SchoolId == school.Id &&
				c.RegisterId.Contains(registerId), ct
			);
	}

	public async Task<IEnumerable<Classroom>> GetClassroomsAsync(School school, Entities.Teacher teacher, CancellationToken ct = default)
	{
		return await _context.Classrooms
			.Where(c
			=> c.SchoolId == school.Id
			&& c.Teachers.Contains(teacher))
			.ToListAsync(ct);
	}

	public async Task<IEnumerable<Classroom>> GetClassroomsForUserAsync(Guid userId, CancellationToken ct = default)
	{
		var studentTask = _context.StudentProfiles.FindAsync([ userId ], ct).AsTask();
		var teacherTask = _context.TeacherProfiles.FindAsync([ userId ], ct).AsTask();
		await Task.WhenAll(studentTask, teacherTask).WaitAsync(ct);

		if (studentTask.Result is not null)
		{
			return [ studentTask.Result.Classroom ];
		}
		else if (teacherTask.Result is not null)
		{
			return teacherTask.Result.Classrooms;
		}
		else
		{
			return [ ];
		}
	}
}
