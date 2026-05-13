using examScheduler.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models.API;
using System.Security.Claims;

namespace examScheduler.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(IAuthService authService) : ControllerBase
{
	private readonly IAuthService _authService = authService;

	[HttpPost]
	public async Task<Result<DateTimeOffset>> Login([FromBody] OAuthRequest request, CancellationToken ct) => await _authService.AuthenticateAsync(request, HttpContext, ct);

	[Route("refresh")]
	[HttpPost]
	public async Task<Result<DateTimeOffset>> Refresh(CancellationToken ct)
	{
		HttpContext.Request.Cookies.TryGetValue(IAuthService.RefreshTokenCookieName, out var refreshToken);
		return refreshToken is null
			? new(System.Net.HttpStatusCode.Unauthorized)
			: await _authService.RefreshTokenAsync(refreshToken, HttpContext, ct);
	}

	[Authorize]
	[Route("me")]
	[HttpGet]
	public async Task<Result<UserProfile>> Me(CancellationToken ct)
	{
		var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (userIdString is null || !Guid.TryParse(userIdString, out var parsedGuid))
		{
			return new(System.Net.HttpStatusCode.Unauthorized);
		}
		return new(await _authService.TryGetUser_AsNoTrackingAsync(parsedGuid, ct), System.Net.HttpStatusCode.Unauthorized, x => x is not null);
	}
}