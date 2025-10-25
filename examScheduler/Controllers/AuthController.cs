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
			var exists = dbContext.UserProfiles.Where(u =>
				u.RegisterUri == registerRequest.RegisterUri &&
				u.RegisterUsername == registerRequest.Username
			).Any();

			if (exists)
			{
				return Conflict();
			}

			return Ok();
		}
		catch (Exception ex) 
		{
			Console.WriteLine(ex.Message);
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