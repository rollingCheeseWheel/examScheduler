using Entities;
using examScheduler.BackgroundServices;
using examScheduler.Data;
using examScheduler.Mappings;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;
using Util.Extensions;

namespace examScheduler.Services;

public interface IScheduleService
{
	Task<Schedule?> GetScheduleAsync_AsNoTracking(Guid actorId, Guid id, CancellationToken ct = default);
	Task<IEnumerable<Schedule>> GetSchedulesForUserAsync_AsNoTracking(Guid userId, CancellationToken ct = default);

	Task<Guid?> TryCreateSchedule(Models.API.ScheduleCreateRequest request, Guid teacherId, CancellationToken ct = default);
	Task<bool> TryDeleteSchedule(Guid scheduleId, Guid actingTeacherId, CancellationToken ct = default);
	Task<bool> TryReportActualStudentsForScheduleSlot(Guid examSlotId, Guid actingTeacherId, IEnumerable<Guid> actualParticipants, CancellationToken ct = default);

	Task<bool> TryEnlistStudentAsync(Guid slotId, Guid actingStudentId, CancellationToken ct = default);

	Task<bool> TryCreateSwapRequestAsync(Guid scheduleId, Guid requestingStudentId, Guid requestedSlotId, CancellationToken ct = default);
	Task<bool> TryDeleteSwapRequestAsync(Guid swapRequestId, Guid owningStudentId, CancellationToken ct = default);
	Task<bool> TryAcceptSwapRequestAsync(Guid swapRequestId, Guid acceptingStudentId, CancellationToken ct = default);

	Task<bool> HasAccessToSchedule(Guid userId, Guid scheduleId, CancellationToken ct = default);
}

public class ScheduleService(
	AppDbContext context,
	IEventWorker eventWorker,
	ILogger<ScheduleService> logger
) : IScheduleService
{
	private readonly AppDbContext _context = context;
	private readonly IEventWorker _eventWorker = eventWorker;
	private readonly ILogger _logger = logger;

	public async Task<bool> HasAccessToSchedule(Guid userId, Guid scheduleId, CancellationToken ct = default)
	{
		var classroom = await _context.Classrooms
			.Include(c => c.Students)
			.Include(c => c.Teachers.Where(t => t.TeacherProfile != null))
				.ThenInclude(t => t.TeacherProfile!)
			.Where(c => c.Schedules.Select(s => s.Id).Contains(scheduleId))
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
		if (classroom.Teachers.Select(t => t.TeacherProfile).WhereNotNull().Select(tp => tp.Id).Contains(userId))
		{
			return true;
		}
		return false;
	}

	public async Task<Schedule?> GetScheduleAsync_AsNoTracking(Guid actorId, Guid id, CancellationToken ct = default)
	{
		if (!await HasAccessToSchedule(actorId, id, ct))
		{
			return null;
		}

		return await _context.Schedules.AsNoTracking().FindByIdAsync(id, ct);
	}

	public async Task<IEnumerable<Schedule>> GetSchedulesForUserAsync_AsNoTracking(Guid userId, CancellationToken ct = default)
	{
		var studentSchedules = await _context.Users
			.AsNoTracking()
			.WhereId(userId)
			.Select(u => u.StudentProfile)
			.WhereNotNull()
			.Select(sp => sp.Classroom)
			.SelectMany(c => c.Schedules)
			.ToListAsync(ct);

		if (studentSchedules.Count != 0)
		{
			return studentSchedules;
		}

		var teacherSchedules = await _context.Users
			.AsNoTracking()
			.WhereId(userId)
			.Select(u => u.TeacherProfile)
			.WhereNotNull()
			.Select(tp => tp.Teacher)
			.WhereNotNull()
			.SelectMany(t => t.Classrooms)
			.SelectMany(c => c.Schedules)
			.ToListAsync(ct);
		return teacherSchedules;
	}

	public async Task<Guid?> TryCreateSchedule(Models.API.ScheduleCreateRequest request, Guid teacherId, CancellationToken ct = default)
	{
		var hasOverLappingSlots = request.Generator.Slots
			.GroupBy(s => s.DayOfWeek)
			.Any(g => g.Count() > 1);
		if (hasOverLappingSlots)
		{
			return null;
		}

		var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Name == request.SubjectName, ct);
		if (subject is null)
		{
			return null;
		}

		var teacher = await _context.Teachers
			.Include(t => t.Classrooms)
			.Where(t => t.TeacherProfile != null && t.TeacherProfile.Id == teacherId)
			.OrderById()
			.FirstOrDefaultAsync(ct);
		if (teacher is null)
		{
			return null;
		}

		var classroom = await _context.Classrooms
			.Include(c => c.Calendar)
			.FindByIdAsync(request.ClassroomId, ct);
		if (classroom is null || !teacher.Classrooms.Contains(classroom))
		{
			return null;
		}

		var calendar = classroom.Calendar;
		if (calendar is null)
		{
			return null;
		}

		var maxCapacity = request.Generator.Slots.Sum(g => g.MaxParticipants);
		// TODO even if the student count is lower than the min capacity it should probably still be considered valid
		if (classroom.Students.Count > maxCapacity)
		{
			return null;
		}

		// check if generatorslots match the calendar
		foreach (var generatorSlot in request.Generator.Slots.DistinctBy(s => s.DayOfWeek))
		{
			var exists = calendar.Lessons
				.Where(l => l.Subject.Name == subject.Name)
				.Where(l => l.DayOfWeek == generatorSlot.DayOfWeek)
				.Any();
			if (!exists)
			{
				return null;
			}
		}

		var newScheduleId = Guid.CreateVersion7();
		var newSchedule = new Schedule
		{
			Id = newScheduleId,
			Subject = subject,
			Description = request.Description,
			ScheduleGenerator = new()
			{
				GeneratorSlots = request.Generator.Slots.Select(x => x.ToEntity()).ToList(),
				BlacklistedDays = request.Generator.BlacklistedDays.ToList()
			},
			//SlotFillingBehaviour = request.SlotFillingBehaviour,
			//AutoLockIn = request.AutoLockIn,
			AutoLockInOffset = request.LockInOffset,
			StartDate = request.StartDate,
			Teachers = classroom.Teachers.Where(t => t.Subjects.Contains(subject)).ToList(),
			ExamSlots = [ ]
		};

		var classroomStudentCount = await _context.Classrooms
			.WhereId(classroom.Id)
			.SelectMany(c => c.Students)
			.CountAsync(ct);

		var isExtendSuccess = newSchedule.TryExtend(classroomStudentCount, out var newSlots);
		if (!isExtendSuccess)
		{
			return null;
		}

		_context.Add(newSchedule);
		classroom.Schedules.Add(newSchedule);
		await _context.SaveChangesAsync(ct);

		foreach (var slot in newSlots)
		{
			_eventWorker.Publish(new LockScheduleTask(newScheduleId), slot.LockInDate);
		}
		return newSchedule.Id;
	}

	public async Task<bool> TryDeleteSchedule(Guid scheduleId, Guid actingTeacherId, CancellationToken ct = default)
	{
		if (!await HasAccessToSchedule(actingTeacherId, scheduleId, ct))
		{
			return false;
		}

		var schedule = await _context.Schedules.FindByIdAsync(scheduleId, ct);
		if (schedule is null)
		{
			return false;
		}
		_context.Remove(schedule);
		await _context.SaveChangesAsync(ct);
		_eventWorker.Publish(new ScheduleRemovedEvent(scheduleId));
		return true;
	}

	public async Task<bool> TryReportActualStudentsForScheduleSlot(Guid examSlotId, Guid actingTeacherId, IEnumerable<Guid> participants, CancellationToken ct = default)
	{
		if (!participants.Any())
		{
			return false;
		}


		var schedule = await _context.Schedules
			.Where(s => s.ExamSlots.Any(s => s.Id == examSlotId))
			.OrderById()
			.FirstOrDefaultAsync(ct);
		if (schedule is null)
		{
			return false;
		}

		if (!await HasAccessToSchedule(actingTeacherId, schedule.Id, ct))
		{
			return false;
		}

		var slot = schedule.ExamSlots.FindById(examSlotId);
		if (slot is null)
		{
			return false;
		}

		var students = await GetAllStudentsExactAsync(participants, ct);
		if (students is null)
		{
			return false;
		}

		var isSuccess = schedule.TryReportStudents(examSlotId, actingTeacherId, students, out var createdExamSlots);
		if (!isSuccess)
		{
			return false;
		}

		await _context.SaveChangesAsync(ct);
		_eventWorker.Publish(new ScheduleUpdatedEvent(schedule.Id));
		foreach (var createdSlot in createdExamSlots)
		{
			_eventWorker.Publish(new LockScheduleTask(createdSlot.Id), createdSlot.LockInDate);
		}

		return true;
	}

	public async Task<bool> TryEnlistStudentAsync(Guid slotId, Guid studentId, CancellationToken ct = default)
	{
		var student = await _context.StudentProfiles.FindByIdAsync(studentId, ct);
		if (student is null)
		{
			return false;
		}

		var schedule = await _context.Schedules
			.Where(s => s.ExamSlots.Any(s => s.Id == slotId))
			.OrderById()
			.FirstOrDefaultAsync(ct);
		if (schedule is null)
		{
			return false;
		}
		if (!await HasAccessToSchedule(studentId, schedule.Id, ct))
		{
			return false;
		}

		if (student.ClassroomId != schedule.ClassroomId)
		{
			return false;
		}

		var isSuccess = schedule.TryEnlistStudent(slotId, student);
		if (!isSuccess)
		{
			return false;
		}
		try
		{
			await _context.SaveChangesAsync(ct);
		}
		catch (DbUpdateConcurrencyException ex)
		{
			_logger.LogError(ex, "exception during save, affected entries: {@entries}", ex.Entries);
		}
		_eventWorker.Publish(new ScheduleUpdatedEvent(schedule.Id));
		return true;
	}

	public async Task<bool> TryCreateSwapRequestAsync(Guid scheduleId, Guid requestingStudentId, Guid requestedSlotId, CancellationToken ct = default)
	{
		if (!await HasAccessToSchedule(requestingStudentId, scheduleId, ct))
		{
			return false;
		}
		var schedule = await _context.Schedules.FindByIdAsync(scheduleId, ct);
		if (schedule is null)
		{
			return false;
		}
		_context.Attach(schedule);

		var requestingStudent = await _context.Users
			.Select(u => u.StudentProfile)
			.WhereNotNull()
			.FindByIdAsync(requestingStudentId, ct);
		if (requestingStudent is null)
		{
			return false;
		}
		_context.Attach(requestingStudent);

		var newSwapRequest = new SwapRequest
		{
			ScheduleId = scheduleId,
			RequestingStudentName = requestingStudent.UserProfile.Name,
			RequestingStudentId = requestingStudentId,
			RequestedSlotId = requestedSlotId,
		};

		_context.Add(newSwapRequest);
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
			.Where(s => s.SwapRequests.Any(s => s.Id == swapRequestId))
			.OrderById()
			.FirstOrDefaultAsync(ct);
		if (schedule is null)
		{
			return false;
		}
		if (!await HasAccessToSchedule(actingStudentId, schedule.Id, ct))
		{
			return false;
		}
		_context.Attach(schedule);

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
		var schedule = await _context.Schedules
			.Where(s => s.SwapRequests.Any(s => s.Id == swapRequestId))
			.OrderById()
			.FirstOrDefaultAsync(ct);
		if (schedule is null)
		{
			return false;
		}
		if (!await HasAccessToSchedule(actingStudentId, schedule.Id, ct))
		{
			return false;
		}
		_context.Attach(schedule);

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
			.DistinctById()
			.ToListAsync(ct);
		return students.Count == ids.Count() ? students : null;
	}
}
