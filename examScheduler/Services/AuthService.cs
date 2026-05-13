using examScheduler.BackgroundServices;
using examScheduler.Data;
using examScheduler.Mappings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Models.API;
using Models.DigitalesRegister;
using registerClient;
using System.Net;
using System.Security.Claims;
using Util;
using Util.Extensions;

namespace examScheduler.Services;

public interface IAuthService
{
	const string AccessTokenCookieName = "access_token";
	const string RefreshTokenCookieName = "refresh_token";

	Task<Result<DateTimeOffset>> AuthenticateAsync(OAuthRequest request, HttpContext httpContext, CancellationToken ct);
	Task<Result<DateTimeOffset>> RefreshTokenAsync(string refreshToken, HttpContext httpContext, CancellationToken ct);

	Task<UserProfile?> TryGetUser_AsNoTrackingAsync(Guid userId, CancellationToken ct = default);
}

public record TokenPair(string AccessToken, string RefreshToken);

public class AuthService(
	AppDbContext context,
	UserManager<Entities.UserProfile> userManager,
	RoleManager<IdentityRole<Guid>> roleManager,
	IClassroomService classroomService,
	ITokenProvider jwtProvider,
	JwtOptions jwtOptions,
	ILogger<AuthService> logger,
	ISchoolsService schoolsService,
	IDigitalRegisterClientService digitalRegisterClientService,
	IEventWorker eventWorker
) : IAuthService
{
	private readonly AppDbContext _context = context;
	private readonly UserManager<Entities.UserProfile> _userManager = userManager;
	private readonly RoleManager<IdentityRole<Guid>> _roleManager = roleManager;
	private readonly IClassroomService _classroomService = classroomService;
	private readonly ITokenProvider _jwtProvider = jwtProvider;
	private readonly JwtOptions _jwtOptions = jwtOptions;
	private readonly ILogger _logger = logger;
	private readonly ISchoolsService _schoolsService = schoolsService;
	private readonly IDigitalRegisterClientService _digitalRegisterClientService = digitalRegisterClientService;
	private readonly IEventWorker _eventWorker = eventWorker;

	public async Task<UserProfile?> TryGetUser_AsNoTrackingAsync(Guid userId, CancellationToken ct = default)
	{
		return ( await _context.Users
			.AsNoTracking()
			.WhereId(userId)
			.FirstOrDefaultAsync(ct) )?.ToDTO();
	}

	public async Task<Result<DateTimeOffset>> AuthenticateAsync(OAuthRequest request, HttpContext httpContext, CancellationToken ct = default)
	{
		using var logginScope = _logger.BeginScope(new { request.SchoolId });
		using var transcation = await _context.Database.BeginTransactionAsync(ct);

		var school = await _schoolsService.GetSchoolBySchoolIdAsync_AsNoTracking(request.SchoolId, ct);
		if (school is null)
		{
			return new(HttpStatusCode.NotFound, "Unknown School ID");
		}

		var registerClient = await _digitalRegisterClientService.TryCreateClientAsync(school.SchoolId, request.AuthCode, ct);
		if (registerClient is null)
		{
			return new(HttpStatusCode.BadRequest, "Unable to log in");
		}

		var userProfile = await registerClient.GetUserProfileAsync(ct);
		if (userProfile is null)
		{
			return new(HttpStatusCode.BadRequest, "Could not fetch user profile");
		}

		var existingUser = await _userManager.Users
			.FirstOrDefaultAsync(u => u.SchoolId == school.SchoolId && u.RegiserId == userProfile.Id, ct);
		Result<DateTimeOffset>? response;
		if (existingUser is not null)
		{
			_logger.LogInformation("User {UserName} found, logging them in", existingUser.Name);
			response = await LoginAsync(existingUser, httpContext, ct);
		}
		else
		{
			_logger.LogInformation("Registering new user");
			response = await RegisterAsync(registerClient, school, httpContext, ct);
		}

		await _context.SaveChangesAsync(ct);
		if (response.Success)
		{
			_logger.LogInformation("Successfully logged in");
			await transcation.CommitAsync(ct);

			var justCreatedUser = await _userManager.Users.FirstOrDefaultAsync(u => u.SchoolId == school.SchoolId && u.RegiserId == userProfile.Id, ct);
			if (justCreatedUser is not null && justCreatedUser.Role is UserRoles.Student)
			{
				await TryPublishCalendarExtendTaskAsync(registerClient.Id, justCreatedUser, ct);
			}
		}
		else
		{
			_logger.LogWarning("Login unsuccessful ({Code}): {@Errors}", response.StatusCode, response.Errors);
			await transcation.RollbackAsync(ct);
		}
		return response;
	}

	public async Task<Result<DateTimeOffset>> RefreshTokenAsync(string refreshToken, HttpContext httpContext, CancellationToken ct = default)
	{
		var token = await _context.RefreshSessions.FirstOrDefaultAsync(s => s.TokenValue == refreshToken, ct);
		if (token is null)
		{
			return new(HttpStatusCode.NotFound);
		}
		var user = await _context.Users.FindByIdAsync(token.UserProfileId, ct);
		if (user is null)
		{
			return new(HttpStatusCode.NotFound);
		}
		var claims = await GetUserClaimsAsync(user, ct);
		var tokens = await _jwtProvider.RefreshTokenPairAsync(claims, refreshToken, user, ct);
		if (tokens is null)
		{
			_logger.LogWarning("Failed to refresh tokens for user {Username}", user.Name);
			return new(HttpStatusCode.Unauthorized);
		}
		else
		{
			_logger.LogInformation("Successfully refreshed tokens for user {Username}", user.Name);
			ConfigureCookies(ref httpContext, tokens);
			return new(DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.TokenExpirationInMinutes));
		}
	}

	private async Task<Result<DateTimeOffset>> LoginAsync(Entities.UserProfile user, HttpContext httpContext, CancellationToken ct = default)
	{
		if (user.TeacherProfile is not null && user.TeacherProfile.Teacher is not null)
		{
			await ConnectTeacherWithCalendarTeacherAsync(user.Id, ct);
		}

		var claims = await GetUserClaimsAsync(user, ct);
		var tokens = await _jwtProvider.CreateTokenPairAsync(claims, user, ct);
		if (tokens is null)
		{
			_logger.LogWarning("Failed to generate tokens for user {Username}", user.Name);
			return new(HttpStatusCode.Unauthorized);
		}
		else
		{
			_logger.LogInformation("Successfully generated tokens for user {Username}", user.Name);
			ConfigureCookies(ref httpContext, tokens);
			return new(DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.RefreshTokenExpirationInMinutes));
			//return new(new AuthResponse
			//{
			//	User = user.ToDTO(),
			//	Expiration = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.RefreshTokenExpirationInMinutes)
			//});
		}
	}

	private void ConfigureCookies(ref HttpContext httpContext, TokenPair? tokens)
	{
		httpContext.Response.Cookies.Delete(IAuthService.AccessTokenCookieName);
		httpContext.Response.Cookies.Delete(IAuthService.RefreshTokenCookieName);

		if (tokens is null)
		{
			return;
		}

		httpContext.Response.Cookies.Append(IAuthService.AccessTokenCookieName, tokens.AccessToken, new()
		{
			HttpOnly = true,
			Secure = true,
			Path = "/",
			Expires = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.TokenExpirationInMinutes),
			SameSite = SameSiteMode.Strict,
		});

		httpContext.Response.Cookies.Append(IAuthService.RefreshTokenCookieName, tokens.RefreshToken, new()
		{
			HttpOnly = true,
			Secure = true,
			Path = "/api/auth/extend",
			Expires = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.RefreshTokenExpirationInMinutes),
			SameSite = SameSiteMode.Strict,
		});
	}

	private async Task<ICollection<Claim>> GetUserClaimsAsync(Entities.UserProfile user, CancellationToken ct = default)
	{
		var roles = await _userManager.GetRolesAsync(user).WaitAsync(ct);

		List<Claim> claims = [
			new(ClaimTypes.NameIdentifier, user.Id.ToString()),
			new(ClaimTypes.GroupSid, user.SchoolId.ToString()),
			new(ClaimTypes.Name, user.Name),
			..roles.Select(r => new Claim(ClaimTypes.Role, r))
		];
		return claims;
	}

	private async Task<Result<DateTimeOffset>> RegisterAsync(ILightWeightDigitalRegisterClient registerClient, Entities.School school, HttpContext httpContext, CancellationToken ct = default) => await registerClient.GetRoleAsync(ct) switch
	{
		UserRoles.Student => await RegisterStudentAsync(registerClient, school, httpContext, ct),
		UserRoles.Teacher => await RegisterTeacherAsync(registerClient, school, httpContext, ct),
		_ => new(HttpStatusCode.BadRequest)
	};

	private async Task<Result<DateTimeOffset>> RegisterStudentAsync(ILightWeightDigitalRegisterClient registerClient, Entities.School school, HttpContext httpContext, CancellationToken ct = default)
	{
		var registerUserProfile = await registerClient.GetUserProfileAsync(ct);
		if (registerUserProfile is null)
		{
			_logger.LogWarning("Could not fetch user profile");
			return new(HttpStatusCode.InternalServerError);
		}

		if (registerUserProfile.StudentData is null ||
			registerUserProfile.StudentData.MainClass is null)
		{
			return new(HttpStatusCode.BadRequest);
		}

		var userProfile = CreateUserProfile(registerUserProfile, school);
		if (userProfile is null ||
			userProfile.Role is not UserRoles.Student)
		{
			_logger.LogError("Could not create user profile");
			return new(HttpStatusCode.InternalServerError);
		}

		var classroom = await _classroomService.GetOrCreateClassroomAsync(school, registerUserProfile, ct);
		if (classroom is null)
		{
			_logger.LogError("Could not get or create classroom");
			return new(HttpStatusCode.InternalServerError);
		}

		var studentProfile = new Entities.StudentProfile
		{
			Classroom = classroom,
			UserProfile = userProfile,
		};

		userProfile.StudentProfile = studentProfile;

		classroom.Students.Add(studentProfile);

		var userCreateResult = await _userManager.CreateAsync(userProfile);
		if (!userCreateResult.Succeeded)
		{
			_logger.LogWarning("Error creating user: {@Reason}", userCreateResult.Errors);
			return new(HttpStatusCode.InternalServerError);
		}

		var role = await EnsureRoleCreatedAsync(userProfile.Role, ct);
		var roleAddedResult = await _userManager.AddToRoleAsync(userProfile, role);
		if (!roleAddedResult.Succeeded)
		{
			_logger.LogWarning("Error creating role: {@Reason}", roleAddedResult.Errors);
			return new(HttpStatusCode.InternalServerError);
		}
		else
		{
			_eventWorker.Publish(new ClassroomStudentCountChangedEvent(classroom.Id), 10);
			_logger.LogInformation("Successfully registered user {Name}, logging them in", userProfile.Name);
			return await LoginAsync(userProfile, httpContext, ct);
		}
	}

	private async Task<Result<DateTimeOffset>> RegisterTeacherAsync(ILightWeightDigitalRegisterClient registerClient, Entities.School school, HttpContext httpContext, CancellationToken ct = default)
	{
		var registerUserProfile = await registerClient.GetUserProfileAsync(ct);
		if (registerUserProfile is null)
		{
			_logger.LogWarning("Unable to fetch user profile");
			return new(HttpStatusCode.InternalServerError);
		}

		if (registerUserProfile.StudentData is not null)
		{
			return new(HttpStatusCode.BadRequest);
		}

		var userProfile = CreateUserProfile(registerUserProfile, school);
		if (userProfile is null ||
			userProfile.Role is not UserRoles.Teacher)
		{
			return new(HttpStatusCode.BadRequest);
		}

		// on each login the connection will be tried to be established
		var existingTeacherProfile = await _context.Teachers
			.Where(t => t.SchoolId == school.SchoolId)
			.Where(t => t.Name == userProfile.Name)
			.FirstOrDefaultAsync(ct);

		var teacherProfile = new Entities.TeacherProfile
		{
			UserProfile = userProfile,
			Teacher = existingTeacherProfile,
		};

		userProfile.TeacherProfile = teacherProfile;

		var userCreateResult = await _userManager.CreateAsync(userProfile);
		if (!userCreateResult.Succeeded)
		{
			_logger.LogWarning("Unable to create user: {Reason}", userCreateResult.Errors.Stringify());
			return new(HttpStatusCode.InternalServerError);
		}

		var role = await EnsureRoleCreatedAsync(userProfile.Role, ct);
		var roleAddedResult = await _userManager.AddToRoleAsync(userProfile, role);
		if (!roleAddedResult.Succeeded)
		{
			_logger.LogWarning("Unable to assign roles to user: {Reason}", roleAddedResult.Errors.Stringify());
			return new(HttpStatusCode.InternalServerError);
		}

		return await LoginAsync(userProfile, httpContext, ct);
	}

	private static Entities.UserProfile? CreateUserProfile(RegisterUserProfile registerUserProfile, Entities.School school)
	{
		var role = IDigitalRegisterClient.GetRole(registerUserProfile);
		if (role is null)
		{
			return null;
		}
		var guid = Guid.CreateVersion7();
		return new()
		{
			Id = guid,
			UserName = guid.ToString(),
			RegiserId = registerUserProfile.Id,
			Name = string.Join(" ", registerUserProfile.FirstName, registerUserProfile.LastName),
			Role = (UserRoles)role!,
			SchoolId = school.SchoolId,
		};
	}

	private async Task<string> EnsureRoleCreatedAsync(UserRoles role, CancellationToken ct = default)
	{
		var roleName = role.ToString();
		var existingRole = await _roleManager.FindByNameAsync(roleName).WaitAsync(ct);
		if (existingRole is null)
		{
			await _roleManager.CreateAsync(new(roleName));
		}
		return roleName;
	}

	private async Task ConnectTeacherWithCalendarTeacherAsync(Guid teacherId, CancellationToken ct = default)
	{
		var teacherProfile = await _context.TeacherProfiles.FindByIdAsync(teacherId, ct);
		if (teacherProfile is null || teacherProfile.Teacher is not null)
		{
			return;
		}

		var teacher = await _context.Teachers
			.Where(t => t.SchoolId == teacherProfile.UserProfile.SchoolId)
			.Where(t => t.Name == teacherProfile.UserProfile.Name)
			.FirstOrDefaultAsync(ct);

		if (teacher is null)
		{
			return;
		}

		teacherProfile.Teacher = teacher;
	}

	private async Task TryPublishCalendarExtendTaskAsync(Guid registerClientId, Entities.UserProfile user, CancellationToken ct = default)
	{
		if (user.Role is not UserRoles.Student || user.StudentProfile is null)
		{
			_logger.LogInformation("User is not a student");
			return;
		}

		_eventWorker.Publish(new ExtendCalendarTask(registerClientId, user.Id), 5);
	}
}
