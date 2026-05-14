using Entities;
using examScheduler.Data;
using Microsoft.EntityFrameworkCore;
using Models.DigitalesRegister;
using Util.Extensions;

namespace examScheduler.Services;

public interface IClassroomService
{
	Task<Classroom?> GetOrCreateClassroomAsync(School school, RegisterUserProfile userProfile, CancellationToken ct = default);
	Task<Classroom?> GetClassroomByRegisterIdAsync(School school, int registerId, CancellationToken ct = default);
	Task<IEnumerable<Classroom>> GetClassroomsForUserAsync_AsNoTracking(Guid userId, CancellationToken ct = default);
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
			SchoolId = school.SchoolId,
		};

		var calendar = new Calendar();

		newClassroom.CalendarId = calendar.Id;
		_context.Add(calendar);
		await _context.Classrooms.AddAsync(newClassroom, ct);
		await _context.SaveChangesAsync(ct);
		return newClassroom;
	}

	public async Task<Classroom?> GetClassroomByRegisterIdAsync(School school, int registerId, CancellationToken ct = default) => await _context.Classrooms
			.FirstOrDefaultAsync(c =>
				c.SchoolId == school.SchoolId &&
				c.RegisterId.Contains(registerId), ct
			);

	public async Task<IEnumerable<Classroom>> GetClassroomsForUserAsync_AsNoTracking(Guid userId, CancellationToken ct = default)
	{
		var studentClassrooms = await _context.StudentProfiles
			.AsNoTracking()
			.WhereId(userId)
			.Select(sp => sp.Classroom)
			.ToListAsync(ct);

		if (studentClassrooms.Count != 0)
		{
			return studentClassrooms;
		}

		var teacherSchedules = await _context.TeacherProfiles
			.AsNoTracking()
			.WhereId(userId)
			.SelectMany(tp => tp.Classrooms)
			.ToListAsync(ct);
		return teacherSchedules;
	}
}
