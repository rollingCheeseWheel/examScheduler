using examScheduler.Digitales_Register_API;
using examScheduler.Models.Auth;
using Microsoft.AspNetCore.Mvc;

namespace examScheduler.Controllers;
[Route("api/[controller]")]
[ApiController]
public class DigitalesRegisterController : ControllerBase
{
	[HttpPost]
	public async Task<IActionResult> GetProfileDetails([FromBody] RegisterRequest request, CancellationToken ct)
	{
		var registerClient = new RegisterClient(request);

		return Ok(await registerClient.GetProfileDetailsAsync(ct));
	}
}
