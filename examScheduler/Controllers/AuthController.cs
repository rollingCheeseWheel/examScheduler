using examScheduler.Models.Auth;
using Microsoft.AspNetCore.Mvc;

namespace examScheduler.Controllers;
[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
	[Route("login")]
	[HttpPost]
	public IActionResult Login([FromBody] LoginRequest loginRequest)
	{
		return Ok("sadfasfd");
	}
}