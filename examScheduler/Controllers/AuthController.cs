using examScheduler.Data;
using examScheduler.Services;
using Microsoft.AspNetCore.Mvc;
using Models.API;

namespace examScheduler.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(
    IAuthService authService,
    AppDbContext context
) : ControllerBase
{
    private readonly IAuthService _authService = authService;
    private readonly AppDbContext _context = context;

    [HttpPost]
    public async Task<Result<UserProfile>> Login([FromBody] OAuthRequest request, CancellationToken ct)
    {
        return await _authService.AuthenticateAsync(request, HttpContext, ct);
    }

    [Route("refresh")]
    [HttpPost]
    public async Task<Result<DateTimeOffset>> Refresh(CancellationToken ct)
    {
        HttpContext.Request.Cookies.TryGetValue(IAuthService.RefreshTokenCookieName, out var refreshToken);
        if (refreshToken is null)
        {
            return new(System.Net.HttpStatusCode.Unauthorized);
        }
        return await _authService.RefreshTokenAsync(refreshToken, HttpContext, ct);
    }
}