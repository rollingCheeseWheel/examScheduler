using Entities;
using examScheduler.BackgroundServices;
using examScheduler.Data;
using Microsoft.EntityFrameworkCore;
using Util.Extensions;

namespace examScheduler.Services;

public interface ICalendarService
{
	Task<bool> TryExtendCalendarAsync(Guid calendarId, string schoolId, IEnumerable<Models.DigitalesRegister.Lesson> lessons, CancellationToken ct = default);
	//Task NormalizeCalendar(Guid calendarId, CancellationToken ct = default);

	Task<IEnumerable<Lesson>?> TryGetWeekContaintingDateAsync(Guid classroomId, DateTimeOffset date, CancellationToken ct = default);
}

public class CalendarService(AppDbContext context, IEventWorker eventWorker) : ICalendarService
{
	private readonly AppDbContext _context = context;
	private readonly IEventWorker _eventWorker = eventWorker;

	public async Task<bool> TryExtendCalendarAsync(Guid calendarId, string schoolId, IEnumerable<Models.DigitalesRegister.Lesson> modelLessons, CancellationToken ct = default)
	{
		var calendar = await _context.Classrooms
			.JoinInnerOnId(_context.Calendars, c => c.CalendarId)
			.FirstOrDefaultAsync(ct);
		if (calendar is null)
		{
			return false;
		}
		_context.Attach(calendar);

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
					Name = string.Join(" ", t.FirstName, t.LastName),
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
					Occurances = [ modelLesson.Date.ToDateOnly() ],
					Subject = trackedSubjects[ modelLesson.Subject.Name ],
					Teachers = modelLesson.Teachers.Select(t => trackedTeachers[ t.Name ]).ToList()
				};
				calendar.Lessons.Add(newLesson);
			}
			else
			{
				existingLesson.Occurances.Add(modelLesson.Date.ToDateOnly());
			}
		}

		await _context.SaveChangesAsync(ct);
		_eventWorker.Publish(new CalendarUpdatedEvent(calendarId), 3);

		return true;
	}

	public async Task<IEnumerable<Lesson>?> TryGetWeekContaintingDateAsync(Guid classroomId, DateTimeOffset date, CancellationToken ct = default)
	{
		var calendar = await _context.Classrooms
			.AsNoTracking()
			.JoinInnerOnId(_context.Calendars, c => c.CalendarId)
			.FirstOrDefaultAsync(ct);
		if (calendar is null)
		{
			return null;
		}

		var mondayDate = date.RoundDownToMonday();

		return calendar.NormalizeOrDefaultToMostCommonLesson_CreatesNewInstances(new(mondayDate, mondayDate.RoundUpTo(DayOfWeek.Sunday))).SelectMany(x => x);
	}
}