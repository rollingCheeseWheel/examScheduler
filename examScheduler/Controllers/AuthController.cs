using examScheduler.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Models.API;

namespace examScheduler.Controllers;

[Route("api/[controller]/")]
[ApiController]
public class AuthController(IAuthService authService) : ControllerBase
{
	private readonly IAuthService _authService = authService;

	[Route("auth")]
	[HttpPost]
	public IActionResult Login([FromBody] OAuthRequest request)
	{
		throw new NotImplementedException();
	}

	[Route("extend")]
	[HttpPost]
	public IActionResult Extend([FromBody] TokenExtendRequest request)
	{
		throw new NotImplementedException();
	}
}