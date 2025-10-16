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
		var exists = dbContext.Students.Where(s => s.RegisterUsername == registerRequest.Username).Any();

		if (exists)
		{
			return BadRequest();
		}

		return Ok();
	}

	[Route("login")]
	[HttpPost]
	public IActionResult Login([FromBody] LoginRequest loginRequest)
	{
		return Ok("sadfasfd");
	}
}