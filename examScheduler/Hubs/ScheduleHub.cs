using examScheduler.Mappings;
using examScheduler.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Models.API;
using System.Net;
using Util;
using Util.Extensions;

namespace examScheduler.Hubs;

/*
	1.	Swap requests target Slots instead of students
	2.	If two students from different Slots want to swap with each others Slots, the swap should resolve instantly	
 */

public interface IScheduleHub
{
	Task<Result> RegisterForSlot(Guid slotId);

	Task<Result> CreateSwapRequest(Guid scheduleId, Guid examSlotId);
	Task<Result> AcceptSwapRequest(Guid swapRequestId);
	Task<Result> DeleteSwapRequest(Guid swaprequestId);

	Task<Result> CreateSchedule(ScheduleCreateRequest request);
	Task<Result> SubscribeSchedule(Guid scheduleId);
	Task<Result> DeleteSchedule(Guid scheduleId);
	Task<Result> ReportStudents(Guid scheduleSlotId, IEnumerable<Guid> actualParticipants);
}

public interface IScheduleClient
{
	Task InitialSchedules(IEnumerable<Schedule> schedules);
	Task ScheduleCreated(Guid scheduleId);
	Task ScheduleUpdated(Schedule schedule);
	Task ScheduleRemoved(Guid scheduleId);

	Task InitialClassrooms(IEnumerable<Classroom> classrooms);
	Task ClassroomUpdated(Classroom classroom);
}

[Authorize]
public class ScheduleHub(
	IScheduleService scheduleService,
	IClassroomService classroomService,
	ILogger<ScheduleHub> logger
) : Hub<IScheduleClient>, IScheduleHub
{
	private readonly IScheduleService _scheduleService = scheduleService;
	private readonly IClassroomService _classroomService = classroomService;
	private readonly ILogger _logger = logger;

	private Guid UserId { get; set; }
	private CancellationToken _ct => Context.ConnectionAborted;

	public override async Task OnConnectedAsync()
	{
		try
		{
			if (Context.User?.Identity?.IsAuthenticated is null || !Context.User.Identity.IsAuthenticated)
			{
				return;
			}

			TryUpdateUserId();
			if (UserId == default)
			{
				return;
			}

			var schedules = await _scheduleService.GetSchedulesForUserAsync_AsNoTracking(UserId, _ct);
			var classrooms = await _classroomService.GetClassroomsForUserAsync_AsNoTracking(UserId, _ct);

			foreach (var schedule in schedules)
			{
				await this.AddToScheduleGroupAsync(schedule.Id, _ct);
			}

			foreach (var classroom in classrooms)
			{
				await this.AddToClassroomGroupAsync(classroom.Id);
			}

			await Clients.Caller.InitialSchedules(schedules.Select(x => x.ToDTO()));
			await Clients.Caller.InitialClassrooms(classrooms.Select(x => x.ToDTO()));

			await base.OnConnectedAsync();

		}
		catch (OperationCanceledException)
		{

		}
	}

	[Authorize(Roles = nameof(UserRoles.Student))]
	public async Task<Result> RegisterForSlot(Guid slotId)
	{
		try
		{
			TryUpdateUserId();
			return await _scheduleService.TryEnlistStudentAsync(slotId, UserId, _ct);
		}
		catch (OperationCanceledException)
		{
			return new(HttpStatusCode.BadRequest);
		}
	}

	[Authorize(Roles = nameof(UserRoles.Student))]
	public async Task<Result> CreateSwapRequest(Guid scheduleId, Guid examSlotId)
	{
		try
		{
			TryUpdateUserId();
			return await _scheduleService.TryCreateSwapRequestAsync(scheduleId, UserId, examSlotId, _ct);
		}
		catch (OperationCanceledException)
		{
			return new(HttpStatusCode.BadRequest);
		}
	}

	[Authorize(Roles = nameof(UserRoles.Student))]
	public async Task<Result> DeleteSwapRequest(Guid swapRequestId)
	{
		try
		{
			TryUpdateUserId();
			return await _scheduleService.TryDeleteSwapRequestAsync(swapRequestId, UserId, _ct);
		}
		catch (OperationCanceledException)
		{
			return new(HttpStatusCode.BadRequest);
		}
	}

	[Authorize(Roles = nameof(UserRoles.Student))]
	public async Task<Result> AcceptSwapRequest(Guid swapRequestId)
	{
		try
		{
			TryUpdateUserId();
			return await _scheduleService.TryAcceptSwapRequestAsync(swapRequestId, UserId, _ct);
		}
		catch (OperationCanceledException)
		{
			return new(HttpStatusCode.BadRequest);
		}
	}

	[Authorize(Roles = nameof(UserRoles.Teacher))]
	public async Task<Result> CreateSchedule(ScheduleCreateRequest request)
	{
		try
		{
			TryUpdateUserId();
			var result = await _scheduleService.TryCreateSchedule(request, UserId, _ct);
			if (result.Success)
			{
				await Clients.ClassroomGroup(request.ClassroomId).ScheduleCreated(result.Data);
			}
			return result.To(true);
		}
		catch (OperationCanceledException)
		{
			return new(HttpStatusCode.BadRequest);
		}
	}

	public async Task<Result> SubscribeSchedule(Guid scheduleId)
	{
		try
		{
			TryUpdateUserId();
			var schedule = await _scheduleService.GetScheduleAsync_AsNoTracking(UserId, scheduleId, _ct);
			if (schedule is null)
			{
				return new(HttpStatusCode.NotFound);
			}
			await this.AddToScheduleGroupAsync(scheduleId, _ct);
			await Clients.Caller.ScheduleUpdated(schedule.ToDTO());
			return new(HttpStatusCode.OK);
		}
		catch (OperationCanceledException)
		{
			return new(HttpStatusCode.BadRequest);
		}
	}

	[Authorize(Roles = nameof(UserRoles.Teacher))]
	public async Task<Result> DeleteSchedule(Guid scheduleId)
	{
		try
		{
			TryUpdateUserId();
			return await _scheduleService.TryDeleteSchedule(scheduleId, UserId, _ct);
		}
		catch (OperationCanceledException)
		{
			return new(HttpStatusCode.BadRequest);
		}
	}

	[Authorize(Roles = nameof(UserRoles.Teacher))]
	public async Task<Result> ReportStudents(Guid scheduleSlotId, IEnumerable<Guid> actualParticipants)
	{
		try
		{
			TryUpdateUserId();
			return await _scheduleService.TryReportActualStudentsForScheduleSlot(scheduleSlotId, UserId, actualParticipants, _ct);
		}
		catch (OperationCanceledException)
		{
			return new(HttpStatusCode.BadRequest);
		}
	}

	private void TryUpdateUserId()
	{
		if (UserId != default)
		{
			return;
		}
		if (Context.User is null)
		{
			return;
		}
		Context.User.TryGetId(out var id);
		UserId = id;
	}
}