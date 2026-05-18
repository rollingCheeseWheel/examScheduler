using Entities;
using examScheduler.BackgroundServices;
using examScheduler.Data;
using Microsoft.EntityFrameworkCore;
using Util.Extensions;

namespace examScheduler.Services;

public interface ICalendarService
{
	Task<bool> TryExtendCalendarAsync(
		Guid calendarId,
		string schoolId,
		IEnumerable<Models.DigitalesRegister.Lesson> lessons,
		CancellationToken ct = default);

	Task<IEnumerable<Lesson>?> TryGetWeekContaintingDateAsync(
		Guid classroomId,
		DateTimeOffset date,
		CancellationToken ct = default);
}

public sealed class CalendarService(
	AppDbContext context,
	IEventWorker eventWorker) : ICalendarService
{
	private readonly AppDbContext _context = context;
	private readonly IEventWorker _eventWorker = eventWorker;

	public async Task<bool> TryExtendCalendarAsync(
		Guid calendarId,
		string schoolId,
		IEnumerable<Models.DigitalesRegister.Lesson> modelLessons,
		CancellationToken ct = default)
	{
		var lessons = modelLessons.ToList();

		var calendar = await _context.Calendars
			.Include(c => c.Lessons)
				.ThenInclude(l => l.Subject)
			.Include(c => c.Lessons)
				.ThenInclude(l => l.Teachers)
					.ThenInclude(t => t.Subjects)
			.FirstOrDefaultAsync(c => c.Id == calendarId, ct);

		if (calendar is null)
		{
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

			_context.Subjects.AddRange(newSubjects);

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
			.Select(t => string.Join(" ", t.FirstName, t.LastName))
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

			_context.Teachers.AddRange(newTeachers);

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
				var teacherName = string.Join(
					" ",
					modelTeacher.FirstName,
					modelTeacher.LastName);

				var teacher = trackedTeachers[ teacherName ];

				if (teacher.Subjects.All(s => s.Name != subject.Name))
				{
					teacher.Subjects.Add(subject);
				}
			}
		}

		// LESSONS

		foreach (var modelLesson in lessons)
		{
			var existingLesson = calendar.Lessons
				.FirstOrDefault(l => l.EqualsModel(modelLesson));

			var occurrence = modelLesson.Date.ToDateOnly();

			if (existingLesson is null)
			{
				var newLesson = new Lesson
				{
					Name = modelLesson.LessonName,
					FromHour = Math.Clamp(modelLesson.FromHour - 1, 0, 23),
					ToHour = Math.Clamp(modelLesson.ToHour - 1, 0, 23),
					Occurances = [ occurrence ],
					Subject = trackedSubjects[ modelLesson.Subject.Name ],
					Teachers = modelLesson.Teachers
						.Select(t =>
						{
							var teacherName = string.Join(
								" ",
								t.FirstName,
								t.LastName);

							return trackedTeachers[ teacherName ];
						})
						.ToList()
				};

				calendar.Lessons.Add(newLesson);
			}
			else
			{
				if (!existingLesson.Occurances.Contains(occurrence))
				{
					existingLesson.Occurances.Add(occurrence);
				}
			}
		}

		await _context.SaveChangesAsync(ct);

		_eventWorker.Publish(
			new CalendarUpdatedEvent(calendarId),
			3);

		return true;
	}

	public async Task<IEnumerable<Lesson>?> TryGetWeekContaintingDateAsync(
		Guid classroomId,
		DateTimeOffset date,
		CancellationToken ct = default)
	{
		var classroom = await _context.Classrooms
			.Include(c => c.Calendar)
				.ThenInclude(c => c.Lessons)
					.ThenInclude(l => l.Subject)
			.Include(c => c.Calendar)
				.ThenInclude(c => c.Lessons)
					.ThenInclude(l => l.Teachers)
			.FirstOrDefaultAsync(c => c.Id == classroomId, ct);

		if (classroom?.Calendar is null)
		{
			return null;
		}

		var monday = date.RoundDownToMonday();

		return classroom.Calendar
			.NormalizeOrDefaultToMostCommonLesson_CreatesNewInstances(
				new(
					monday,
					monday.RoundUpTo(DayOfWeek.Sunday)))
			.SelectMany(x => x);
	}
}