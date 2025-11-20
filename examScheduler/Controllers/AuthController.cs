using examScheduler.Data;
using examScheduler.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.API;
using registerClient;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace examScheduler.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(IAuthService authService, AppDbContext context) : ControllerBase
{
	private readonly IAuthService _authService = authService;
	private readonly AppDbContext _context = context;

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

	[Route("profile")]
	[HttpPost]
	public async Task<IActionResult> GetProfile([FromBody] OAuthRequest request, CancellationToken ct)
	{
		var existingSchool = await _context.Schools.FirstOrDefaultAsync(s => s.ClientId == request.SchoolId, ct);

		if (existingSchool is null)
		{
			return BadRequest();
		}

		using var client = new RegisterClient(existingSchool, request.AuthCode);

		return Ok(await client.GetUserProfileAsync(ct));
	}
}