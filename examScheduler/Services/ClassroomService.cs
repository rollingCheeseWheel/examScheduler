using Entities;
using examScheduler.Data;
using Microsoft.EntityFrameworkCore;

namespace examScheduler.Services;

public interface IClassroomService
{
	Task<Classroom?> GetOrCreateClassroomAsync(School school, Models.DigitalesRegister.Calendar calendar, CancellationToken ct);
}

public class ClassroomService(AppDbContext context) : IClassroomService
{
	private readonly AppDbContext _context = context;

	private async Task<Classroom?> GetClassroomAsync(School school, int registerId, CancellationToken ct) => await _context.Classrooms.FirstOrDefaultAsync(c => c.RegisterId == registerId, ct);

	private async Task<ICollection<Teacher>> GetOrCreateTeachersAsync(School school, ICollection<Teacher> teachers, CancellationToken ct )
	{
		// Load existing teachers (with subjects) for the given school through classrooms
		var existingTeachersRaw = await _context.Classrooms
			.Where(c => c.School == school)
			.SelectMany(c => c.Teachers)
			.Include(t => t.Subjects)
			.ToListAsync(ct);

		// De-duplicate tracked teachers (a teacher can be in multiple classrooms)
		var existingTeachersByRegisterId = existingTeachersRaw
			.GroupBy(t => t.RegisterID)
			.Select(g => g.First())
			.ToDictionary(t => t.RegisterID);

		// Cache existing subjects to allow reuse (avoid multiple Subject instances with same RegisterId)
		var existingSubjectsByRegisterId = existingTeachersByRegisterId
			.Values
			.SelectMany(t => t.Subjects)
			.GroupBy(s => s.RegisterId)
			.Select(g => g.First())
			.ToDictionary(s => s.RegisterId);

		var result = new List<Teacher>();

		// Process incoming teachers (distinct by RegisterID)
		foreach (var incoming in teachers
			.GroupBy(t => t.RegisterID)
			.Select(g => g.First()))
		{
			if (existingTeachersByRegisterId.TryGetValue(incoming.RegisterID, out var trackedTeacher))
			{
				// Merge subjects
				foreach (var subj in incoming.Subjects
					.GroupBy(s => s.RegisterId)
					.Select(g => g.First()))
				{
					if (!trackedTeacher.Subjects.Any(s => s.RegisterId == subj.RegisterId))
					{
						if (!existingSubjectsByRegisterId.TryGetValue(subj.RegisterId, out var trackedSubject))
						{
							trackedSubject = new Subject { Name = subj.Name, RegisterId = subj.RegisterId };
							existingSubjectsByRegisterId[trackedSubject.RegisterId] = trackedSubject;
						}
						trackedTeacher.Subjects.Add(trackedSubject);
					}
				}

				result.Add(trackedTeacher);
			}
			else
			{
				// Create new tracked teacher
				var newTeacher = new Teacher
				{
					RegisterID = incoming.RegisterID,
					FirstName = incoming.FirstName,
					LastName = incoming.LastName,
					Subjects = [ ] // fill next
				};

				foreach (var subj in incoming.Subjects
					.GroupBy(s => s.RegisterId)
					.Select(g => g.First()))
				{
					if (!existingSubjectsByRegisterId.TryGetValue(subj.RegisterId, out var trackedSubject))
					{
						trackedSubject = new Subject { Name = subj.Name, RegisterId = subj.RegisterId };
						existingSubjectsByRegisterId[trackedSubject.RegisterId] = trackedSubject;
					}
					newTeacher.Subjects.Add(trackedSubject);
				}

				_context.Add(newTeacher); // ensure EF tracks the new teacher
				existingTeachersByRegisterId[newTeacher.RegisterID] = newTeacher;
				result.Add(newTeacher);
			}
		}

		return result;
	}

	public async Task<Classroom?> GetOrCreateClassroomAsync(School school, Models.DigitalesRegister.Calendar calendar, CancellationToken ct)
	{
		// Parse the incoming register calendar
		var parsedCalendar = Calendar.Parse(calendar, out var subjects, out var teachers, out var classroomInfo);
		if (classroomInfo is null || parsedCalendar is null)
			return null;

		// Resolve teachers to tracked entities (and ensure their subjects exist/are merged)
		var trackedTeachers = await GetOrCreateTeachersAsync(school, teachers.ToList(), ct);
		var teacherByRegisterId = trackedTeachers.ToDictionary(t => t.RegisterID);

		// Rewire parsed lessons to use tracked Teacher/Subject instances
		foreach (var week in parsedCalendar.Weeks)
		{
			foreach (var day in week.Days)
			{
				foreach (var hid in day.HoursInDay)
				{
					var lesson = hid.Lesson;

					// Map lesson teachers to tracked ones (distinct by RegisterID)
					var mappedTeachers = new List<Teacher>();
					foreach (var lt in lesson.Teachers.GroupBy(t => t.RegisterID).Select(g => g.First()))
					{
						if (teacherByRegisterId.TryGetValue(lt.RegisterID, out var tracked))
							mappedTeachers.Add(tracked);
					}
					lesson.Teachers = mappedTeachers;

					// Point lesson.Subject to a tracked subject instance if available
					var subjRegId = lesson.Subject.RegisterId;
					var trackedSubject = lesson.Teachers
						.SelectMany(t => t.Subjects)
						.FirstOrDefault(s => s.RegisterId == subjRegId);

					if (trackedSubject is not null)
					{
						lesson.Subject = trackedSubject;
					}
					else if (lesson.Teachers.FirstOrDefault() is { } firstT
						&& !firstT.Subjects.Any(s => s.RegisterId == subjRegId))
					{
						// Fallback: attach subject to the first tracked teacher for this lesson
						var newSubject = new Subject { Name = lesson.Subject.Name, RegisterId = subjRegId };
						firstT.Subjects.Add(newSubject);
						lesson.Subject = newSubject;
					}
				}
			}
		}

		// Try to fetch existing classroom
		var classroom = await GetClassroomAsync(school, classroomInfo.Value.ClassroomId, ct);
		if (classroom is not null)
		{
			// Ensure required navigations are loaded
			await _context.Entry(classroom).Collection(c => c.Calendars).LoadAsync(ct);
			await _context.Entry(classroom).Collection(c => c.Teachers).LoadAsync(ct);

			// Link teachers to classroom
			foreach (var t in trackedTeachers)
			{
				if (!classroom.Teachers.Any(x => x.RegisterID == t.RegisterID))
					classroom.AddTeacher(t);
			}

			parsedCalendar.Classroom = classroom;

			// Add or update calendar
			var matchedCalendar = classroom.Calendars.FirstOrDefault(c => c == parsedCalendar);
			if (matchedCalendar is null)
			{
				classroom.AddCalendar(parsedCalendar);
			}
			else
			{
				matchedCalendar.Rescanned();
			}

			return classroom;
		}

		// Create a new classroom
		classroom = new Classroom
		{
			Name = classroomInfo.Value.Name,
			RegisterId = classroomInfo.Value.ClassroomId,
			School = school,
			Calendars = [ parsedCalendar ],
			Teachers = trackedTeachers.ToList()
		};

		parsedCalendar.Classroom = classroom;
		return classroom;
	}
}
