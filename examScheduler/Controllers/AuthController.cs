using Models.Auth;
using Microsoft.AspNetCore.Mvc;
using examScheduler.Data;
using Util;

namespace examScheduler.Controllers;
[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
	[Route("register")]
	[HttpPost]
	public IActionResult Register([FromBody] RegisterRequest registerRequest, AppDbContext dbContext)
	{
		try
		{

			var exists = dbContext.Students.Where(s => s.RegisterUsername == registerRequest.Username).Any();




			if (exists)
			{
				return Conflict();
			}

			return Ok();
		}
		catch
		{
			return this.ServerError();
		}
	}

	[Route("login")]
	[HttpPost]
	public IActionResult Login([FromBody] LoginRequest loginRequest)
	{
		return Ok("sadfasfd");
	}
}