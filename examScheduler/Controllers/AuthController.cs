using Microsoft.AspNetCore.Mvc;
using examScheduler.Data;
using Models.API;

namespace examScheduler.Controllers;
[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
	[Route("register")]
	[HttpPost]
	public IActionResult Register([FromBody] AuthRequest registerRequest, AppDbContext dbContext)
	{
		throw new NotImplementedException();
	}

	[Route("login")]
	[HttpPost]
	public IActionResult Login([FromBody] AuthRequest loginRequest)
	{
		throw new NotImplementedException();
	}
}