using Models.Auth;
using Microsoft.AspNetCore.Mvc;
using examScheduler.Data;

namespace examScheduler.Controllers;
[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
	[Route("register")]
	[HttpPost]
	public IActionResult Register([FromBody] RegisterRequest registerRequest, AppDbContext dbContext)
	{
		throw new NotImplementedException();
	}

	[Route("login")]
	[HttpPost]
	public IActionResult Login([FromBody] LoginRequest loginRequest)
	{
		throw new NotImplementedException();
	}
}