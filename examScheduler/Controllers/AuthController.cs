using examScheduler.Services;
using Microsoft.AspNetCore.Mvc;
using Models.API;

namespace examScheduler.Controllers;
[Route("api/[controller]")]
[ApiController]
public class AuthController(IAuthService authService) : ControllerBase
{
	private readonly IAuthService _authService = authService;

	[Route("register")]
	[HttpPost]
	public async Task<IActionResult> Register([FromBody] SignupRequest request, CancellationToken ct)
	{
		return await _authService.RegisterAsync(request, ct);
	}

	[Route("reset")]
	[HttpPost]
	public async Task<IActionResult> PasswordReset([FromBody] SignupRequest request, CancellationToken ct)
	{
		return await _authService.ResetPasswordAsync(request, ct);
	}

	[Route("login")]
	[HttpPost]
	public IActionResult Login([FromBody] AuthRequest loginRequest)
	{
		throw new NotImplementedException();
	}
}