using Microsoft.AspNetCore.Mvc;
using Models.API;

namespace examScheduler.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CalendarsController
{
    [HttpGet]
    public async Task<Result<Dictionary<Guid, Calendar>>> GetCalendarsAsync(CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
