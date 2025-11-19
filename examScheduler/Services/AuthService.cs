using Entities;
using examScheduler.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Models.API;
using registerClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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
	IKeyVaultService keyVaultService,
	IServiceProvider serviceProvider
) : IAuthService
{
	private readonly AppDbContext _context = context;
	private readonly UserManager<UserProfile> _userManager = userManager;
	private readonly RoleManager<IdentityRole<int>> _roleManager = roleManager;
	private readonly IClassroomService _classroomService = classroomService;
	private readonly IKeyVaultService _keyVaultService = keyVaultService;
	private readonly IServiceProvider _serviceProvider = serviceProvider;

	public async Task<GenericResponse<TokenResponse>> AuthenticateAsync(OAuthRequest request, CancellationToken ct = default)
	{
		// verify that the school exists
		var existingSchool = await _context.Schools
			.Include(s => s.RegisterUri)
			.FirstOrDefaultAsync(s => s.SchoolId == request.SchoolId, ct);
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

		var tokenOptions = _serviceProvider.GetRequiredService<TokenOptions>();

		var existingUser = _context.UserProfiles
			.FirstOrDefault(p => p.UserName == userProfile.ToString() && p.SchoolId == existingSchool.Id);
		if (existingUser is not null) // user found 
		{
			//var claims = [
			//	new Claim(ClaimTypes.NameIdentifier, existingUser.Id)
			//];

			//new JwtSecurityToken()
		}

		//using var client = new RegisterClient();
		throw new NotImplementedException();
	}

	public Task<GenericResponse<TokenResponse>> ExtendTokenAsync(TokenExtendRequest request, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}
}
