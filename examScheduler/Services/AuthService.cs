using Entities;
using examScheduler.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Models.API;
using Models.DigitalesRegister;
using registerClient;
using System.Net;
using System.Security.Claims;
using System.Transactions;

namespace examScheduler.Services;

public interface IAuthService
{
	Task<Result<TokenResponse>> AuthenticateAsync(OAuthRequest request, CancellationToken ct);
	Task<Result<TokenResponse>> ExtendTokenAsync(TokenExtendRequest request, CancellationToken ct);
}

public class AuthService(
	AppDbContext context,
	UserManager<UserProfile> userManager,
	RoleManager<IdentityRole<Guid>> roleManager,
	IClassroomService classroomService,
	IServiceProvider serviceProvider,
	ITokenProvider jwtProvider
) : IAuthService
{
	private readonly AppDbContext _context = context;
	private readonly UserManager<UserProfile> _userManager = userManager;
	private readonly RoleManager<IdentityRole<Guid>> _roleManager = roleManager;
	private readonly IClassroomService _classroomService = classroomService;
	private readonly IServiceProvider _serviceProvider = serviceProvider;
	private readonly ITokenProvider _jwtProvider = jwtProvider;

	public async Task<Result<TokenResponse>> AuthenticateAsync(OAuthRequest request, CancellationToken ct = default)
	{
		using var transcation = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

		// verify that the school exists
		var school = await _context.Schools
			.Include(s => s.RegisterUri)
			.FirstOrDefaultAsync(s => s.SchoolId == request.SchoolId, ct);
		if (school is null)
		{
			return new("Unknown School ID", HttpStatusCode.BadRequest);
		}

		// create the client and fetch the userprofile
		using var client = new RegisterClient(school, request.AuthCode);
		var userProfile = await client.GetUserProfileAsync(ct);
		if (userProfile is null)
		{
			return new("Could not fetch user profile", HttpStatusCode.InternalServerError);
		}

		var existingUser = await _userManager.Users
			.Include(u => u.StudentProfile)
			.Include(u => u.TeacherProfile)
			.FirstOrDefaultAsync(u => u.SchoolId == school.Id && u.UserName == userProfile.Id.ToString(), ct);
		Result<TokenResponse>? response = null;
		if (existingUser is not null) // user found 
		{
			response = await LoginAsync(existingUser, ct);
		}

		response = await RegisterAsync(client, request, school, ct);
		if (response.Success)
		{
			transcation.Complete();
		}
		await _context.SaveChangesAsync(ct);
		return response;
	}

	public async Task<Result<TokenResponse>> ExtendTokenAsync(TokenExtendRequest request, CancellationToken ct = default)
	{
		var token = await _context.RefreshSessions.FirstOrDefaultAsync(s => s.TokenValue == request.RefreshToken, ct);
		if (token is null) { return new(HttpStatusCode.NotFound); }
		var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == token.UserProfileId, ct);
		if (user is null) { return new(HttpStatusCode.NotFound); }
		var claims = await GetUserClaimsAsync(user, ct);
		var response = await _jwtProvider.RefreshTokenPairAsync(claims, request.RefreshToken, user, ct);
		return new(response, HttpStatusCode.Unauthorized);
	}

	private async Task<Result<TokenResponse>> LoginAsync(UserProfile user, CancellationToken ct = default)
	{
		using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
		var claims = await GetUserClaimsAsync(user, ct);
		var tokenResponse = await _jwtProvider.GetTokenPairAsync(claims, user, ct);
		return new(tokenResponse, HttpStatusCode.Unauthorized);
	}

	private async Task<ICollection<Claim>> GetUserClaimsAsync(Guid userId, CancellationToken ct = default)
	{
		var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
		return user is null ? [ ] : await GetUserClaimsAsync(user, ct);
	}

	private async Task<ICollection<Claim>> GetUserClaimsAsync(UserProfile user, CancellationToken ct = default)
	{
		var roles = await _userManager.GetRolesAsync(user);

		List<Claim> claims = [
			new(ClaimTypes.NameIdentifier, user.Id.ToString()),
			new(ClaimTypes.GroupSid, user.SchoolId.ToString()),
			new(ClaimTypes.Name, user.FirstName),
			new(ClaimTypes.Surname, user.LastName),
			..roles.Select(r => new Claim(ClaimTypes.Role, r))
		];
		return claims;
	}

	private async Task<Result<TokenResponse>> RegisterAsync(RegisterClient registerClient, OAuthRequest request, Entities.School school, CancellationToken ct = default)
	{
		return await registerClient.GetRoleAsync(ct) switch
		{
			UserProfileRole.Student => await RegisterStudentAsync(registerClient, request, school, ct),
			UserProfileRole.Teacher => await RegisterTeacherAsync(registerClient, request, school, ct),
			_ => new(HttpStatusCode.BadRequest)
		};
	}

	private async Task<Result<TokenResponse>> RegisterStudentAsync(RegisterClient registerClient, OAuthRequest request, Entities.School school, CancellationToken ct = default)
	{
		var registerUserProfile = await registerClient.GetUserProfileAsync(ct);
		if (registerUserProfile is null || // could not fetch
			registerUserProfile.StudentData is null || // doesnt have student data
			registerUserProfile.StudentData.MainClass is null) // doesnt have a main class asigned
		{
			return new(HttpStatusCode.InternalServerError);
		}

		var userProfile = await CreateUserProfileAsync(registerUserProfile, school, ct);
		if (userProfile is null ||
			userProfile.Role is not UserProfileRole.Student)
		{
			return new(HttpStatusCode.InternalServerError);
		}

		// TODO: add classroom creation, signup etc
		var classroom = await _classroomService.GetOrCreateClassroomAsync(school, registerUserProfile, ct);
		
		


		throw new NotImplementedException();
	}

	private async Task<Result<TokenResponse>> RegisterTeacherAsync(RegisterClient registerClient, OAuthRequest request, Entities.School school, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	private async Task<UserProfile?> CreateUserProfileAsync(RegisterUserProfile registerUserProfile, Entities.School school, CancellationToken ct = default)
	{
		var role = RegisterClient.GetRole(registerUserProfile);
		if (role is null) { return null; }
		var userProfile = new UserProfile
		{
			RegiserId = registerUserProfile.Id,
			FirstName = registerUserProfile.FirstName,
			LastName = registerUserProfile.LastName,
			Role = (UserProfileRole)role!,
			School = school,
		};
		return userProfile;
	}
}
