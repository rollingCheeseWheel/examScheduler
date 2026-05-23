using examScheduler.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models.API;
using System.Net;
using System.Security.Claims;

namespace examScheduler.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(IAuthService authService) : ControllerBase
{
	private readonly IAuthService _authService = authService;

	[Route("login")]
	[HttpPost]
	public async Task<Result<DateTimeOffset>> Login([FromBody] OAuthRequest request, CancellationToken ct) => await _authService.AuthenticateAsync(request, ct);

	[Route("logout")]
	[HttpGet]
	public async Task<IActionResult> Logout(CancellationToken ct)
	{
		await HttpContext.SignOutAsync();
		return Ok();
	}

	[Authorize]
	[Route("me")]
	[HttpGet]
	public async Task<Result<UserProfile>> Me(CancellationToken ct)
	{
		var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
		return userIdString is null || !Guid.TryParse(userIdString, out var parsedGuid)
			? new(null, HttpStatusCode.Unauthorized)
			: new(await _authService.TryGetUser_AsNoTrackingAsync(parsedGuid, ct), HttpStatusCode.Unauthorized, x => x is not null);
	}
}