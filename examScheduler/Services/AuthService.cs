using Entities;
using examScheduler.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Models.API;
using registerClient;
using System.Net;
using System.Security.Claims;
using Util;

namespace examScheduler.Services;

public interface IAuthService
{
	Task<GenericResponse<TokenResponse>> AuthenticateAsync(OAuthRequest request, CancellationToken ct);
	Task<GenericResponse<TokenResponse>> ExtendTokenAsync(TokenExtendRequest request, CancellationToken ct);
}

public class AuthService(
	AppDbContext context,
	UserManager<UserProfile> userManager,
	RoleManager<IdentityRole<Guid>> roleManager,
	IClassroomService classroomService,
	IServiceProvider serviceProvider,
	ITokenProvider jwtService
) : IAuthService
{
	private readonly AppDbContext _context = context;
	private readonly UserManager<UserProfile> _userManager = userManager;
	private readonly RoleManager<IdentityRole<Guid>> _roleManager = roleManager;
	private readonly IClassroomService _classroomService = classroomService;
	private readonly IServiceProvider _serviceProvider = serviceProvider;
	private readonly ITokenProvider _jwtService = jwtService;

	public async Task<GenericResponse<TokenResponse>> AuthenticateAsync(OAuthRequest request, CancellationToken ct = default)
	{
		// verify that the school exists
		var existingSchool = await _context.Schools
			.Include(s => s.RegisterUri)
			.FirstOrDefaultAsync(s => s.SchoolId == request.SchoolId, ct);
		if (existingSchool is null)
		{
			return new("Unknown School ID", HttpStatusCode.BadRequest);
		}

		// create the client and fetch the userprofile
		using var client = new RegisterClient(existingSchool, request.AuthCode);
		var userProfile = await client.GetUserProfileAsync(ct);
		if (userProfile is null)
		{
			return new("Could not fetch user profile", HttpStatusCode.InternalServerError);
		}

		var existingUser = await _userManager.Users
			.Include(u => u.StudentProfile)
			.Include(u => u.TeacherProfile)
			.FirstOrDefaultAsync(u => u.SchoolId == existingSchool.Id && u.UserName == userProfile.Id.ToString(), ct);
		if (existingUser is not null) // user found 
		{
			return await LoginAsync(existingUser, ct);
		}

		return await RegisterAsync(client, request, ct);
	}

	public async Task<GenericResponse<TokenResponse>> ExtendTokenAsync(TokenExtendRequest request, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	private async Task<GenericResponse<TokenResponse>> LoginAsync(UserProfile user, CancellationToken ct = default)
	{
		var roles = await _userManager.GetRolesAsync(user);

		List<Claim> claims = [
			new(ClaimTypes.NameIdentifier, user.Id.ToString()),
			new(ClaimTypes.GroupSid, user.SchoolId.ToString()),
			new(ClaimTypes.Name, user.FirstName),
			new(ClaimTypes.Surname, user.LastName),
			..roles.Select(r => new Claim(ClaimTypes.Role, r))
		];

		var tokenResponse = await _jwtService.GetTokenPairAsync(claims, user, ct);
		if (tokenResponse is null)
		{
			return new(HttpStatusCode.Unauthorized);
		}
		return new(tokenResponse);
	}

	private async Task<GenericResponse<TokenResponse>> RegisterAsync(RegisterClient registerClient, OAuthRequest request, CancellationToken ct = default)
	{
		return await registerClient.GetRoleAsync(ct) switch
		{
			UserProfileRole.Student => await RegisterStudentAsync(registerClient, request, ct),
			UserProfileRole.Teacher => await RegisterTeacherAsync(registerClient, request, ct),
			_ => new(HttpStatusCode.BadRequest)
		};
	}

	private async Task<GenericResponse<TokenResponse>> RegisterStudentAsync(RegisterClient registerClient, OAuthRequest request, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	private async Task<GenericResponse<TokenResponse>> RegisterTeacherAsync(RegisterClient registerClient, OAuthRequest request, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}
}
