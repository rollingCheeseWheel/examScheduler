using examScheduler.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Models.API;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace examScheduler.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(IAuthService authService) : ControllerBase
{
	private readonly IAuthService _authService = authService;

	[HttpPost]
	public async Task<IActionResult> Login([FromBody] OAuthRequest request, CancellationToken ct)
	{
		return await _authService.AuthenticateAsync(request, ct);
	}

	[Route("extend")]
	[HttpPost]
	public async Task<IActionResult> Extend([FromBody] TokenExtendRequest request, CancellationToken ct)
	{
		return await _authService.ExtendTokenAsync(request, ct);
	}
}