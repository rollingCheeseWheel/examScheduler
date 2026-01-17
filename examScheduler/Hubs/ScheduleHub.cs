using examScheduler.Mappings;
using examScheduler.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Models.API;
using System.Net;
using System.Security.Claims;

namespace examScheduler.Hubs;

public interface IScheduleHub
{
    Task<Result<bool>> RegisterForSlot(Guid scheduleId, Guid slotId);

    Task<Result<bool>> CreateSwapRequest(Guid scheduleId, Guid userId);
    Task<Result<bool>> AcceptSwapRequest(Guid swapRequestId);
}

public interface IScheduleClient
{
    Task ReceiveSchedules(Schedule[ ] schedules);
    Task UpdateSchedule(Guid scheduleId, Schedule schedule);

    Task ReceiveSwapRequest(SwapRequest swapRequest);
    Task ReceiveInitialSwapRequests(IEnumerable<SwapRequest> swapRequests);
}

[Authorize]
public class ScheduleHub(
    IScheduleService scheduleService,
    IStudentService studentService
) : Hub<IScheduleClient>, IScheduleHub
{
    private readonly IScheduleService _scheduleService = scheduleService;
    private readonly IStudentService _studentService = studentService;

    private Guid _guid = default;
    private bool _isGuidSet = false;

    private CancellationToken _ct => Context.ConnectionAborted;

    public override async Task OnConnectedAsync()
    {
        var claimsPrincipal = Context.User;
        var stringedUserId = claimsPrincipal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (claimsPrincipal is null ||
            claimsPrincipal.Identity?.IsAuthenticated is null ||
            !claimsPrincipal.Identity.IsAuthenticated)
        {
            return;
        }

        if (!Guid.TryParse(stringedUserId, out var userId))
        {
            return;
        }
        _isGuidSet = true;
        _guid = userId;

        var scheduleIds = await _scheduleService.GetScheduleIdsForStudentAsync(userId, _ct);
        foreach (var scheduleId in scheduleIds)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, scheduleId.ToString(), _ct);
        }

        await TransmitBacklogAsync(userId, _ct);

        await base.OnConnectedAsync();
    }

    public async Task<Result<bool>> RegisterForSlot(Guid scheduleId, Guid slotId)
    {
        if (!_isGuidSet) return new(HttpStatusCode.Unauthorized);

        return new(
            await _scheduleService.TryEnlistStudentAsync(scheduleId, slotId, _guid, _ct),
            HttpStatusCode.BadRequest,
            b => b);
    }

    public async Task<Result<bool>> CreateSwapRequest(Guid scheduleId, Guid userId)
    {
        if (!_isGuidSet) return new(HttpStatusCode.Unauthorized);

        var swapRequest = await _scheduleService.CreateSwapRequestAsync(scheduleId, _guid, userId, DateTimeOffset.UtcNow.AddDays(30), _ct);

        if (swapRequest is not null)
        {
            await Clients.User(userId.ToString())
                .ReceiveSwapRequest(swapRequest.ToDTO());
            return new(true);
        }
        return new(HttpStatusCode.BadRequest);
    }

    public async Task<Result<bool>> AcceptSwapRequest(Guid swapRequestId)
    {
        return new(
            await _scheduleService.AcceptSwapRequestAsync(swapRequestId, _ct),
            HttpStatusCode.BadRequest,
            (b) => b
        );
    }

    private async Task TransmitBacklogAsync(Guid userId, CancellationToken ct = default)
    {
        var swapRequests = ( await _scheduleService.GetSwapRequestsForStudentAsync(userId, ct) )
            .Select(SwapRequestMappings.ToDTO);
        await Clients.Caller.ReceiveInitialSwapRequests(swapRequests);
    }
}