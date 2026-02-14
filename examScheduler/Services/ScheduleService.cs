using Entities;
using examScheduler.BackgroundServices;
using examScheduler.Data;
using examScheduler.Mappings;
using Microsoft.EntityFrameworkCore;
using Util.Extensions;

namespace examScheduler.Services;

public interface IScheduleService
{
	Task<Schedule?> GetScheduleAsync(Guid id, CancellationToken ct = default);
	Task<IEnumerable<Schedule>> GetSchedulesForUserAsync_AsNoTracking(Guid userId, CancellationToken ct = default);

	Task<bool> TryCreateSchedule(Models.API.ScheduleCreateRequest request, Guid teacherId, CancellationToken ct = default);
	Task<bool> TryDeleteSchedule(Guid scheduleId, Guid actingTeacherId, CancellationToken ct = default);
	Task<bool> TryReportActualStudentsForScheduleSlot(Guid examSlotId, Guid teacherId, IEnumerable<Models.API.UserProfile> actualParticipants, CancellationToken ct = default);

	Task<bool> TryEnlistStudentAsync(Guid slotId, Guid actingStudentId, CancellationToken ct = default);

	Task<bool> TryCreateSwapRequestAsync(Guid scheduleId, Guid requestingStudentId, Guid requestedSlotId, CancellationToken ct = default);
	Task<bool> TryDeleteSwapRequestAsync(Guid swapRequestId, Guid owningStudentId, CancellationToken ct = default);
	Task<bool> TryAcceptSwapRequestAsync(Guid swapRequestId, Guid acceptingStudentId, CancellationToken ct = default);
}

public class ScheduleService(AppDbContext context, IEventWorker eventWorker) : IScheduleService
{
	private readonly AppDbContext _context = context;
	private readonly IEventWorker _eventWorker = eventWorker;

	public async Task<Schedule?> GetScheduleAsync(Guid id, CancellationToken ct = default) => await _context.Classrooms.SelectMany(c => c.Schedules).FindByIdAsync(id, ct);

	public async Task<IEnumerable<Schedule>> GetSchedulesForUserAsync_AsNoTracking(Guid userId, CancellationToken ct = default)
	{
		var user = await _context.Users
			.AsNoTracking()
			.WhereId(userId)
			.Select(u => new
			{
				u.TeacherProfile,
				u.StudentProfile
			})
			.FirstOrDefaultAsync(ct);
		return user?.StudentProfile?.Classroom.Schedules ?? user?.TeacherProfile?.Classrooms.SelectMany(c => c.Schedules) ?? [ ];
	}

	public async Task<IEnumerable<Guid>> GetScheduleIdsForStudentAsync_AsNoTracking(Guid userId, CancellationToken ct = default) => await _context.Users
			.Select(u => u.StudentProfile)
			.WhereNotNull()
			.AsNoTracking()
			.Where(sp => sp.Id == userId)
			.SelectMany(sp => sp.Classroom.Schedules)
			.Select(s => s.Id)
			.ToListAsync(ct);

	public async Task<bool> TryCreateSchedule(Models.API.ScheduleCreateRequest request, Guid teacherId, CancellationToken ct = default)
	{
		var hasOverLappingSlots = request.Generator.Slots
			.GroupBy(s => s.DayOfWeek)
			.Any(g => g.Count() > 1);
		if (hasOverLappingSlots)
		{
			return false;
		}

		var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Name == request.SubjectName, ct);
		if (subject is null)
		{
			return false;
		}

		var teacherProfile = await _context.Users
			.Select(u => u.TeacherProfile)
			.WhereNotNull()
			.FindByIdAsync(teacherId, ct);
		if (teacherProfile is null || teacherProfile.Teacher is null) { return false; }

		var classroom = await _context.Classrooms.FindByIdAsync(request.ClassroomId, ct);
		if (classroom is null || !teacherProfile.Classrooms.Contains(classroom))
		{
			return false;
		}

		var minCapacity = request.Generator.Slots.Sum(g => g.MinParticipants);
		var maxCapacity = request.Generator.Slots.Sum(g => g.MaxParticipants);
		if (classroom.Students.Count < minCapacity ||
			classroom.Students.Count > maxCapacity)
		{
			return false;
		}

		foreach (var generatorSlot in request.Generator.Slots.DistinctBy(s => s.DayOfWeek))
		{
			var exists = classroom.Calendar.Lessons
				.Where(l => l.Subject.Name == subject.Name)
				.Where(l => l.DayOfWeek == generatorSlot.DayOfWeek)
				.Any();
			if (!exists)
			{
				return false;
			}
		}

		var actualLessonDates = await _context.Classrooms
			.WhereId(classroom.Id)
			.Select(c => c.Calendar)
			.SelectMany(c => c.Lessons)
			.Where(l => l.Subject.Name == subject.Name)
			.SelectMany(l => l.Occurances)
			.ToListAsync(ct);

		var newScheduleId = Guid.NewGuid();
		var newSchedule = new Schedule
		{
			Id = newScheduleId,
			Subject = subject,
			Description = request.Description,
			ScheduleGenerator = new()
			{
				GeneratorSlots = [ .. request.Generator.Slots.Select(x => x.ToEntity()) ],
				BlacklistedDays = [ .. actualLessonDates.Intersect(request.Generator.BlacklistedDays) ]
			},
			SlotFillingBehaviour = request.SlotFillingBehaviour,
			AutoLockIn = request.AutoLockIn,
			AutoLockInOffset = request.LockInOffset,
			StartDate = request.StartDate,
			Teachers = [ .. classroom.Teachers.Where(t => t.Subjects.Contains(subject)) ],
			ExamSlots = [ ]
		};

		var newSlots = newSchedule.Extend(classroom.Students.Count);

		classroom.Schedules.Add(newSchedule);
		await _context.SaveChangesAsync(ct);

		_eventWorker.Publish(new ScheduleUpdatedEvent(newSchedule.Id));
		foreach (var slot in newSlots)
		{
			_eventWorker.Publish(new LockScheduleTask(newScheduleId), slot.LockInDate);
		}
		return true;
	}

	public async Task<bool> TryDeleteSchedule(Guid scheduleId, Guid actingTeacherId, CancellationToken ct = default)
	{
		var teacher = await _context.TeacherProfiles.FindByIdAsync(actingTeacherId, ct);
		if (teacher is null || teacher.Teacher is null)
		{
			return false;
		}

		var schedule = await _context.Classrooms
			.SelectMany(c => c.Schedules)
			.FindByIdAsync(scheduleId, ct);
		if (schedule is null)
		{
			return false;
		}

		if (!schedule.Teachers.ContainsId(teacher.Teacher.Id))
		{
			return false;
		}

		_context.Remove(schedule);
		await _context.SaveChangesAsync(ct);
		_eventWorker.Publish(new ScheduleRemovedEvent(schedule.Id));
		return true;
	}

	public async Task<bool> TryReportActualStudentsForScheduleSlot(Guid examSlotId, Guid teacherId, IEnumerable<Models.API.UserProfile> participants, CancellationToken ct = default)
	{
		var schedule = await _context.Classrooms
			.SelectMany(c => c.Schedules)
			.Where(s => s.ExamSlots.ContainsId(examSlotId))
			.FirstOrDefaultAsync(ct);
		if (schedule is null)
		{
			return false;
		}

		var slot = schedule.ExamSlots.FindById(examSlotId);
		if (slot is null)
		{
			return false;
		}

		var students = await GetAllStudentsExactAsync(participants.Select(x => x.Id), ct);
		if (students is null || !students.Any())
		{
			return false;
		}

		var isSuccess = schedule.TryReportStudents(examSlotId, teacherId, students);
		if (!isSuccess)
		{
			return false;
		}

		await _context.SaveChangesAsync(ct);
		_eventWorker.Publish(new ScheduleUpdatedEvent(schedule.Id));
		return true;
	}

	public async Task<bool> TryEnlistStudentAsync(Guid slotId, Guid studentId, CancellationToken ct = default)
	{
		var student = await _context.Users
			.Select(u => u.StudentProfile)
			.WhereNotNull()
			.FindByIdAsync(studentId, ct);
		if (student is null)
		{
			return false;
		}

		var schedule = await _context.Classrooms
			.SelectMany(c => c.Schedules)
			.Where(s => s.ExamSlots.ContainsId(slotId))
			.FirstOrDefaultAsync(ct);
		if (schedule is null)
		{
			return false;
		}

		var isSuccess = schedule.TryEnlistStudent(slotId, student);
		if (!isSuccess)
		{
			return false;
		}
		await _context.SaveChangesAsync(ct);
		_eventWorker.Publish(new ScheduleUpdatedEvent(schedule.Id));
		return true;
	}

	public async Task<bool> TryCreateSwapRequestAsync(Guid scheduleId, Guid requestingStudentId, Guid requestedSlotId, CancellationToken ct = default)
	{
		var schedule = await _context.Classrooms.SelectMany(c => c.Schedules).FindByIdAsync(scheduleId, ct);
		if (schedule is null)
		{
			return false;
		}

		var requestingStudent = await _context.Users
			.Select(u => u.StudentProfile)
			.WhereNotNull()
			.FindByIdAsync(requestingStudentId, ct);
		if (requestingStudent is null)
		{
			return false;
		}

		var newSwapRequest = new SwapRequest
		{
			ScheduleId = scheduleId,
			RequestingStudentName = requestingStudent.UserProfile.Name,
			RequestingStudentId = requestingStudentId,
			RequestedSlotId = requestedSlotId,
		};

		var isSuccess = schedule.TryAddSwapRequest(newSwapRequest);
		schedule.ResolveImplicitSwaps();

		await _context.SaveChangesAsync(ct);
		_eventWorker.Publish(new ScheduleUpdatedEvent(schedule.Id));
		return true;
	}

	public async Task<bool> TryDeleteSwapRequestAsync(Guid swapRequestId, Guid actingStudentId, CancellationToken ct = default)
	{
		var schedule = await _context.Classrooms
			.SelectMany(c => c.Schedules)
			.Where(s => s.SwapRequests.ContainsId(swapRequestId))
			.FirstOrDefaultAsync(ct);

		if (schedule is null)
		{
			return false;
		}

		var isSuccess = schedule.TryDeleteSwapRequest(swapRequestId);
		if (!isSuccess)
		{
			return false;
		}

		await _context.SaveChangesAsync(ct);
		_eventWorker.Publish(new ScheduleUpdatedEvent(schedule.Id));
		return true;
	}

	public async Task<bool> TryAcceptSwapRequestAsync(Guid swapRequestId, Guid actingStudentId, CancellationToken ct = default)
	{
		var schedule = await _context.Classrooms
			.SelectMany(c => c.Schedules)
			.Where(s => s.SwapRequests.ContainsId(swapRequestId))
			.FirstOrDefaultAsync(ct);
		if (schedule is null)
		{
			return false;
		}

		var swappingResult = schedule.TryAcceptSwapRequest(swapRequestId, actingStudentId);
		if (!swappingResult)
		{
			return false;
		}

		await _context.SaveChangesAsync(ct);
		_eventWorker.Publish(new ScheduleUpdatedEvent(schedule.Id));
		return true;
	}

	private async Task<IEnumerable<StudentProfile>?> GetAllStudentsExactAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
	{
		var students = await _context.Users
			.Select(u => u.StudentProfile)
			.WhereNotNull()
			.WhereIds(ids)
			.ToListAsync(ct);
		return students.Count == ids.Count() ? students : null;
	}
}
