using Entities;
using examScheduler.Data;
using Microsoft.EntityFrameworkCore;
using Util;

namespace examScheduler.Services;

public interface ICalendarService
{
	Task<bool> TryExtendCalendar(Guid calendarId, Guid schoolId, IEnumerable<Models.DigitalesRegister.Lesson> lessons, CancellationToken ct = default);
	//Task NormalizeCalendar(Guid calendarId, CancellationToken ct = default);
}

public class CalendarService(AppDbContext context) : ICalendarService
{
	private readonly AppDbContext _context = context;

	public async Task<bool> TryExtendCalendar(Guid calendarId, Guid schoolId, IEnumerable<Models.DigitalesRegister.Lesson> modelLessons, CancellationToken ct = default)
	{
		var calendar = await _context.Calendars.FindAsync([ calendarId ], ct);
		if (calendar is null)
		{
			return false;
		}

		// trackedSubjects[subjectName]
		// Get or create subjects, ensure they're being tracked
		var trackedSubjects = calendar.Lessons
			.Select(x => x.Subject)
			.Distinct()
			.ToDictionary(x => x.Name);
		var subjectNames = modelLessons.Select(l => l.Subject.Name).Distinct();

		var missingSubjectKeys = subjectNames.Except(trackedSubjects.Keys).ToList();
		var fetchedSubjects = await _context.Subjects
			.Where(s => missingSubjectKeys.Contains(s.Name))
			.ToListAsync(ct);
		foreach (var s in fetchedSubjects)
		{
			trackedSubjects[ s.Name ] = s;
		}
		var subjectKeysToCreate = missingSubjectKeys.Except(fetchedSubjects.Select(s => s.Name));
		var createdSubjects = subjectKeysToCreate
			.Select(n => new Subject(n)).ToList();
		_context.Subjects.AddRange(createdSubjects);
		foreach (var s in createdSubjects)
		{
			trackedSubjects[ s.Name ] = s;
		}

		// trackedTeachers[teacherName]
		// Get or create teachers, ensure they're being tracked
		var trackedTeachers = calendar.Lessons
			.SelectMany(l => l.Teachers)
			.Distinct()
			.ToDictionary(t => t.Name);
		var teacherNames = modelLessons
			.SelectMany(l => l.Teachers)
			.Select(t => t.Name)
			.Distinct();

		var missingTeacherKeys = teacherNames.Except(trackedTeachers.Keys).ToList();
		var fetchedTeachers = await _context.Teachers
			.Where(t => t.SchoolId == schoolId)
			.Where(t => missingTeacherKeys.Contains(t.Name))
			.ToListAsync(ct);
		foreach (var t in fetchedTeachers)
		{
			trackedTeachers[ t.Name ] = t;
		}
		var teacherNamesToCreate = missingTeacherKeys.Except(fetchedTeachers.Select(t => t.Name)).ToList();
		var createdTeachers = modelLessons
			.SelectMany(l => l.Teachers)
			.Where(t => teacherNames.Contains(t.Name))
			.GroupBy(t => t.Name)
			.Select(g =>
			{
				var t = g.First();
				return new Teacher
				{
					FirstName = t.FirstName,
					LastName = t.LastName,
					SchoolId = schoolId
				};
			})
			.ToList();
		_context.Teachers.AddRange(createdTeachers);
		foreach (var t in createdTeachers)
		{
			trackedTeachers[ t.Name ] = t;
		}

		// Assign subjects to teachers
		foreach (var modelLesson in modelLessons)
		{
			var subject = trackedSubjects[ modelLesson.Subject.Name ];
			foreach (var modelTeacher in modelLesson.Teachers)
			{
				var teacher = trackedTeachers[ modelTeacher.Name ];
				if (!teacher.Subjects.Contains(subject))
				{
					teacher.Subjects.Add(subject);
				}
			}
		}

		// create new lessons or update occurances
		foreach (var modelLesson in modelLessons)
		{
			var existingLesson = calendar.Lessons
				.Where(l => l.EqualsModel(modelLesson))
				.FirstOrDefault();
			if (existingLesson is null)
			{
				var newLesson = new Lesson
				{
					Name = modelLesson.LessonName,
					FromHour = Math.Clamp(modelLesson.FromHour - 1, 0, 23),
					ToHour = Math.Clamp(modelLesson.ToHour - 1, 0, 23),
					Occurances = [ modelLesson.Date ],
					Subject = trackedSubjects[ modelLesson.Subject.Name ],
					Teachers = modelLesson.Teachers.Select(t => trackedTeachers[ t.Name ]).ToList()
				};
			}
			else
			{
				existingLesson.Occurances.Add(modelLesson.Date);
			}
		}

		await _context.SaveChangesAsync(ct);

		return true;
	}
}