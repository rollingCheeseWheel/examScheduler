using examScheduler.BackgroundServices;
using examScheduler.Mappings;
using examScheduler.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Models.API;
using OpenTelemetry.Exporter;
using System.Net;
using System.Security.Claims;
using Util;
using Util.Extensions;

namespace examScheduler.Hubs;

/*
	1.	Swap requests target Slots instead of students
	2.	If two students from different Slots want to swap with each others Slots, the swap should resolve instantly	
 */

public interface IScheduleHub
{
	Task<Result<bool>> RegisterForSlot(Guid slotId);

	Task<Result<bool>> CreateSwapRequest(Guid scheduleId, Guid examSlotId);
	Task<Result<bool>> AcceptSwapRequest(Guid swapRequestId);
	Task<Result<bool>> DeleteSwapRequest(Guid swaprequestId);

	Task<Result<bool>> CreateSchedule(ScheduleCreateRequest request);
	Task<Result<bool>> SubscribeSchedule(Guid scheduleId);
	Task<Result<bool>> DeleteSchedule(Guid scheduleId);
	Task<Result<bool>> ReportStudents(Guid scheduleSlotId, IEnumerable<Guid> actualParticipants);
}

public interface IScheduleClient
{
	Task ReceiveInitialSchedules(IEnumerable<Schedule> schedules);
	Task ScheduleCreated(Guid scheduleId);
	Task UpdateSchedule(Schedule schedule);
	Task RemoveSchedule(Guid scheduleId);

	Task ReceiveInitialClassrooms(IEnumerable<Classroom> classrooms);
	Task UpdateClassroom(Classroom classroom);
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

			await Clients.Caller.ReceiveInitialSchedules(schedules.Select(x => x.ToDTO()));
			await Clients.Caller.ReceiveInitialClassrooms(classrooms.Select(x => x.ToDTO()));

			await base.OnConnectedAsync();

		}
		catch (OperationCanceledException)
		{

		}
	}

	[Authorize(Roles = nameof(UserRoles.Student))]
	public async Task<Result<bool>> RegisterForSlot(Guid slotId)
	{
		try
		{
			TryUpdateUserId();
			var result = await _scheduleService.TryEnlistStudentAsync(slotId, UserId, _ct);
			return new(result, HttpStatusCode.BadRequest, result);
		}
		catch (OperationCanceledException)
		{
			return new(HttpStatusCode.BadRequest);
		}
	}

	[Authorize(Roles = nameof(UserRoles.Student))]
	public async Task<Result<bool>> CreateSwapRequest(Guid scheduleId, Guid examSlotId)
	{
		try
		{
			TryUpdateUserId();
			var result = await _scheduleService.TryCreateSwapRequestAsync(scheduleId, UserId, examSlotId, _ct);
			return new(result, HttpStatusCode.BadRequest, result);
		}
		catch (OperationCanceledException)
		{
			return new(HttpStatusCode.BadRequest);
		}
	}

	[Authorize(Roles = nameof(UserRoles.Student))]
	public async Task<Result<bool>> DeleteSwapRequest(Guid swapRequestId)
	{
		try
		{
			TryUpdateUserId();
			var result = await _scheduleService.TryDeleteSwapRequestAsync(swapRequestId, UserId, _ct);
			return new(result, HttpStatusCode.BadRequest, result);
		}
		catch (OperationCanceledException)
		{
			return new(HttpStatusCode.BadRequest);
		}
	}

	[Authorize(Roles = nameof(UserRoles.Student))]
	public async Task<Result<bool>> AcceptSwapRequest(Guid swapRequestId)
	{
		try
		{
			TryUpdateUserId();
			var result = await _scheduleService.TryAcceptSwapRequestAsync(swapRequestId, UserId, _ct);
			return new(result, HttpStatusCode.BadRequest, result);
		}
		catch (OperationCanceledException)
		{
			return new(HttpStatusCode.BadRequest);
		}
	}

	[Authorize(Roles = nameof(UserRoles.Teacher))]
	public async Task<Result<bool>> CreateSchedule(ScheduleCreateRequest request)
	{
		try
		{
			TryUpdateUserId();
			var result = await _scheduleService.TryCreateSchedule(request, UserId, _ct);
			return new(result, HttpStatusCode.BadRequest, result);
		}
		catch (OperationCanceledException)
		{
			return new(HttpStatusCode.BadRequest);
		}
	}

	public async Task<Result<bool>> SubscribeSchedule(Guid scheduleId)
	{
		try
		{
			TryUpdateUserId();
			var schedule = await _scheduleService.GetScheduleAsync_AsNoTracking(UserId, scheduleId, _ct);
			if (schedule is null)
			{
				return new(HttpStatusCode.Unauthorized);
			}
			await this.AddToScheduleGroupAsync(scheduleId, _ct);
			await Clients.Caller.UpdateSchedule(schedule.ToDTO());
			return new(true);
		}
		catch (OperationCanceledException)
		{
			return new(HttpStatusCode.BadRequest);
		}
	}

	[Authorize(Roles = nameof(UserRoles.Teacher))]
	public async Task<Result<bool>> DeleteSchedule(Guid scheduleId)
	{
		try
		{
			TryUpdateUserId();
			var result = await _scheduleService.TryDeleteSchedule(scheduleId, UserId, _ct);
			return new(result, HttpStatusCode.BadRequest, result);
		}
		catch (OperationCanceledException)
		{
			return new(HttpStatusCode.BadRequest);
		}
	}

	[Authorize(Roles = nameof(UserRoles.Teacher))]
	public async Task<Result<bool>> ReportStudents(Guid scheduleSlotId, IEnumerable<Guid> actualParticipants)
	{
		try
		{
			TryUpdateUserId();
			var result = await _scheduleService.TryReportActualStudentsForScheduleSlot(scheduleSlotId, UserId, actualParticipants, _ct);
			return new(result, HttpStatusCode.BadRequest, x => x);
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
		var _ = Guid.TryParse(Context.UserIdentifier, out var parsed);
		UserId = parsed;
	}
}