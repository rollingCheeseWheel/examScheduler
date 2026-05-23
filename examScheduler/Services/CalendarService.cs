using Entities;
using examScheduler.BackgroundServices;
using examScheduler.Data;
using examScheduler.Mappings;
using Microsoft.EntityFrameworkCore;
using Util.Extensions;

namespace examScheduler.Services;

public interface ICalendarService
{
	Task<bool> TryExtendCalendarAsync(Guid calendarId, string schoolId, IEnumerable<Models.DigitalesRegister.Lesson> lessons, CancellationToken ct = default);

	Task<IEnumerable<Lesson>?> TryGetWeekContaintingDateAsync(Guid actingTeacherId, Guid classroomId, DateTimeOffset date, CancellationToken ct = default);

	Task<bool> HasAccessToCalendarAsync(Guid userId, Guid calendarId, CancellationToken ct = default);
}

public sealed class CalendarService(
	AppDbContext context,
	IEventWorker eventWorker,
	ILogger<CalendarService> logger
) : ICalendarService
{
	private readonly AppDbContext _context = context;
	private readonly IEventWorker _eventWorker = eventWorker;
	private readonly ILogger _logger = logger;

	public async Task<bool> TryExtendCalendarAsync(Guid calendarId, string schoolId, IEnumerable<Models.DigitalesRegister.Lesson> modelLessons, CancellationToken ct = default)
	{
		var lessons = modelLessons.ToList();

		var classroom = await _context.Classrooms
			.Where(c => c.CalendarId == calendarId)
			.OrderById()
			.FirstOrDefaultAsync(ct);

		if (classroom is null)
		{
			_logger.LogError("classroom not found");
			return false;
		}

		var calendar = await _context.Calendars
			.Include(c => c.Lessons)
				.ThenInclude(l => l.Subject)
			.Include(c => c.Lessons)
				.ThenInclude(l => l.Teachers)
					.ThenInclude(t => t.Subjects)
			.FindByIdAsync(calendarId, ct);

		if (calendar is null)
		{
			_logger.LogError("calendar not found");
			return false;
		}

		// SUBJECTS
		var trackedSubjects = calendar.Lessons
			.Select(l => l.Subject)
			.Where(s => s is not null)
			.DistinctBy(s => s.Name)
			.ToDictionary(s => s.Name);

		var subjectNames = lessons
			.Select(l => l.Subject.Name)
			.Distinct()
			.ToList();

		var missingSubjectNames = subjectNames
			.Except(trackedSubjects.Keys)
			.ToList();

		if (missingSubjectNames.Count > 0)
		{
			var existingSubjects = await _context.Subjects
				.Where(s => missingSubjectNames.Contains(s.Name))
				.ToListAsync(ct);

			foreach (var subject in existingSubjects)
			{
				trackedSubjects[ subject.Name ] = subject;
			}

			var existingNames = existingSubjects
				.Select(s => s.Name)
				.ToHashSet();

			var newSubjects = missingSubjectNames
				.Where(n => !existingNames.Contains(n))
				.Select(n => new Subject(n))
				.ToList();

			await _context.Subjects.AddRangeAsync(newSubjects, ct);

			foreach (var subject in newSubjects)
			{
				trackedSubjects[ subject.Name ] = subject;
			}
		}

		// TEACHERS
		var trackedTeachers = calendar.Lessons
			.SelectMany(l => l.Teachers)
			.DistinctBy(t => t.Name)
			.ToDictionary(t => t.Name);

		var teacherNames = lessons
			.SelectMany(l => l.Teachers)
			.Select(t => t.Name)
			.Distinct()
			.ToList();

		var missingTeacherNames = teacherNames
			.Except(trackedTeachers.Keys)
			.ToList();

		if (missingTeacherNames.Count > 0)
		{
			var existingTeachers = await _context.Teachers
				.Include(t => t.Subjects)
				.Where(t => t.SchoolId == schoolId)
				.Where(t => missingTeacherNames.Contains(t.Name))
				.ToListAsync(ct);

			foreach (var teacher in existingTeachers)
			{
				trackedTeachers[ teacher.Name ] = teacher;
			}

			var existingNames = existingTeachers
				.Select(t => t.Name)
				.ToHashSet();

			var newTeachers = missingTeacherNames
				.Where(n => !existingNames.Contains(n))
				.Select(n => new Teacher
				{
					Name = n,
					SchoolId = schoolId
				})
				.ToList();

			await _context.Teachers.AddRangeAsync(newTeachers, ct);

			foreach (var teacher in newTeachers)
			{
				trackedTeachers[ teacher.Name ] = teacher;
			}
		}

		// ASSIGN SUBJECTS
		foreach (var modelLesson in lessons)
		{
			var subject = trackedSubjects[ modelLesson.Subject.Name ];

			foreach (var modelTeacher in modelLesson.Teachers)
			{
				var teacher = trackedTeachers[ modelTeacher.Name ];

				if (!teacher.Subjects.Select(s => s.Name).Contains(subject.Name))
				{
					teacher.Subjects.Add(subject);
				}
			}
		}

		// assign teachers to classrooms
		foreach (var (_, teacher) in trackedTeachers)
		{
			if (!classroom.Teachers.Contains(teacher))
			{
				classroom.Teachers.Add(teacher);
			}
		}

		// LESSONS
		foreach (var modelLesson in lessons)
		{
			var existingLesson = calendar.Lessons
				.FirstOrDefault(l => EqualsModel(l, modelLesson));

			var occurrence = modelLesson.Date.ToDateOnly();

			if (existingLesson is null)
			{
				var newLesson = new Lesson
				{
					LessonName = modelLesson.LessonName,
					FromHour = Math.Clamp(modelLesson.FromHour - 1, 0, 23),
					ToHour = Math.Clamp(modelLesson.ToHour - 1, 0, 23),
					Occurances = [ occurrence ],
					Subject = trackedSubjects[ modelLesson.Subject.Name ],
					Teachers = modelLesson.Teachers
						.Select(t => trackedTeachers[ t.Name ])
						.ToList()
				};

				calendar.Lessons.Add(newLesson);
				_context.Entry(newLesson).State = EntityState.Added;
			}
			else
			{

				if (!existingLesson.Occurances.Contains(occurrence))
				{
					existingLesson.Occurances.Add(occurrence);
				}
			}
		}

		var lastLessonDate = calendar.Lessons
			.SelectMany(l => l.Occurances)
			.Concat([ DateOnly.MinValue ]) // only a safeguard, might not be needed
			.Max()
			.ToDateTimeOffset();
		calendar.LastsUntil = lastLessonDate;

		await _context.SaveChangesAsync(ct);
		_eventWorker.Publish(new CalendarUpdatedEvent(calendarId), 3);
		return true;
	}

	public async Task<IEnumerable<Lesson>?> TryGetWeekContaintingDateAsync(Guid actingTeacherId, Guid classroomId, DateTimeOffset date, CancellationToken ct = default)
	{
		var calendar = await _context.Calendars
			.Where(c => c.Classroom.Id == classroomId)
			.OrderById()
			.FirstOrDefaultAsync(ct);

		if (calendar is null)
		{
			return null;
		}

		if (!await HasAccessToCalendarAsync(actingTeacherId, calendar.Id, ct))
		{
			return null;
		}

		var monday = date.RoundDownToMonday();

		return calendar
			.NormalizeOrDefaultToMostCommonLesson_CreatesNewInstances(new(monday, monday.RoundUpTo(DayOfWeek.Sunday)))
			.SelectMany(x => x)
			.ToList();
	}

	public async Task<bool> HasAccessToCalendarAsync(Guid userId, Guid calendarId, CancellationToken ct = default)
	{
		var classroom = await _context.Classrooms
			.Include(c => c.Students)
			.Include(c => c.Teachers.Where(t => t.TeacherProfile != null))
				.ThenInclude(t => t.TeacherProfile!)
			.Where(c => c.CalendarId == calendarId)
			.OrderById()
			.FirstOrDefaultAsync(ct);

		if (classroom is null)
		{
			return false;
		}

		if (classroom.Students.Select(s => s.Id).Contains(userId))
		{
			return true;
		}
		if (classroom.Teachers.Select(t => t.TeacherProfile).WhereNotNull().Select(t => t.Id).Contains(userId))
		{
			return true;
		}
		return false;
	}

	private static bool EqualsModel(Lesson entity, Models.DigitalesRegister.Lesson model) =>
		entity.DayOfWeek == model.Date.DayOfWeek &&
		entity.FromHour == model.FromHour - 1 &&
		entity.ToHour == model.ToHour - 1 &&
		entity.Subject.Name == model.Subject.Name &&
		entity.LessonName == model.LessonName;
}