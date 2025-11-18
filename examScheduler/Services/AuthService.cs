using Entities;
using examScheduler.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Models.API;
using registerClient;

namespace examScheduler.Services;

public interface IAuthService
{
	Task<GenericResponse<TokenResponse>> AuthenticateAsync(OAuthRequest request, CancellationToken ct);
	Task<GenericResponse<TokenResponse>> ExtendTokenAsync(TokenExtendRequest request, CancellationToken ct);
}

public class AuthService(
	AppDbContext context,
	UserManager<UserProfile> userManager,
	RoleManager<IdentityRole<int>> roleManager,
	IClassroomService classroomService,
	IKeyVaultService keyVaultService
) : IAuthService
{
	private readonly AppDbContext _context = context;
	private readonly UserManager<UserProfile> _userManager = userManager;
	private readonly RoleManager<IdentityRole<int>> _roleManager = roleManager;
	private readonly IClassroomService _classroomService = classroomService;
	private readonly IKeyVaultService _keyVaultService = keyVaultService;

	public async Task<GenericResponse<TokenResponse>> AuthenticateAsync(OAuthRequest request, CancellationToken ct = default)
	{
		// verify that the school exists
		var existingSchool = await _context.Schools.FirstOrDefaultAsync(s => s.SchoolId == request.SchoolId, ct);
		if (existingSchool is null)
		{
			return new("Unknown School ID", System.Net.HttpStatusCode.BadRequest);
		}

		// get the oAuthSecret
		var oAuthSecret = await _keyVaultService.GetAsync(request.SchoolId, ct);
		if (oAuthSecret is null)
		{
			return new(System.Net.HttpStatusCode.InternalServerError);
		}

		// create the client and fetch the userprofile
		using var client = new RegisterClient(existingSchool.RegisterUri, request.SchoolId, oAuthSecret, request.AuthCode);
		var userProfile = await client.GetUserProfileAsync(ct);
		if (userProfile is null)
		{
			return new("Could not fetch user profile", System.Net.HttpStatusCode.InternalServerError);
		}

		/*var existingStudent = _context.UserProfiles.FirstOrDefault(p => p.)*/

		/*using var client = new RegisterClient();*/
		throw new NotImplementedException();
	}

	public Task<GenericResponse<TokenResponse>> ExtendTokenAsync(TokenExtendRequest request, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}
}
