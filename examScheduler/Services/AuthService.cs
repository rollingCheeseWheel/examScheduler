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

namespace examScheduler.Services;

public interface IAuthService
{
	const string AccessTokenCookieName = "access_token";
	const string RefreshTokenCookieName = "refresh_token";

	Task<Result<UserProfile>> AuthenticateAsync(OAuthRequest request, HttpContext httpContext, CancellationToken ct);
	Task<Result<DateTimeOffset>> RefreshTokenAsync(string refreshToken, HttpContext httpContext, CancellationToken ct);
}

public class AuthService(
	AppDbContext context,
	UserManager<Entities.UserProfile> userManager,
	RoleManager<IdentityRole<Guid>> roleManager,
	IClassroomService classroomService,
	ITokenProvider jwtProvider,
	JwtOptions jwtOptions,
	ILogger<AuthService> logger
) : IAuthService
{
	private readonly AppDbContext _context = context;
	private readonly UserManager<Entities.UserProfile> _userManager = userManager;
	private readonly RoleManager<IdentityRole<Guid>> _roleManager = roleManager;
	private readonly IClassroomService _classroomService = classroomService;
	private readonly ITokenProvider _jwtProvider = jwtProvider;
	private readonly JwtOptions _jwtOptions = jwtOptions;
	private readonly ILogger _logger = logger;

	public async Task<Result<UserProfile>> AuthenticateAsync(OAuthRequest request, HttpContext httpContext, CancellationToken ct = default)
	{
		using var transcation = await _context.Database.BeginTransactionAsync(ct);

		// verify that the school exists
		var school = await _context.Schools
			.FirstOrDefaultAsync(s => s.SchoolId == request.SchoolId, ct);
		if (school is null)
		{
			return new("Unknown School ID", HttpStatusCode.BadRequest);
		}

		// create the registerClient and fetch the userprofile
		using var registerClient = new RegisterClient(school, request.AuthCode);
		var userProfile = await registerClient.GetUserProfileAsync(ct);
		if (userProfile is null)
		{
			return new("Could not fetch user profile", HttpStatusCode.InternalServerError);
		}

		var existingUser = await _userManager.Users
			.Include(u => u.StudentProfile)
			.Include(u => u.TeacherProfile)
			.FirstOrDefaultAsync(u => u.SchoolId == school.Id && u.UserName == userProfile.Id.ToString(), ct);
		Result<UserProfile>? response = null;
		if (existingUser is not null) // user found 
		{
			_logger.LogInformation("User {UserName} found, logging them in", existingUser.Name);
			response = await LoginAsync(registerClient, existingUser, httpContext, ct);
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
		}
		else
		{
			_logger.LogWarning("Unsuccessfully logged user in: {Reason}", response.Errors.ToJson());
		}
		return response;
	}

	public async Task<Result<DateTimeOffset>> RefreshTokenAsync(string refreshToken, HttpContext httpContext, CancellationToken ct = default)
	{
		var token = await _context.RefreshSessions.FirstOrDefaultAsync(s => s.TokenValue == refreshToken, ct);
		if (token is null) { return new(HttpStatusCode.NotFound); }
		var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == token.UserProfileId, ct);
		if (user is null) { return new(HttpStatusCode.NotFound); }
		var claims = await GetUserClaimsAsync(user, ct);
		var tokens = await _jwtProvider.RefreshTokenPairAsync(claims, refreshToken, user, ct);
		if (tokens is null) { return new(HttpStatusCode.Unauthorized); }
		ConfigureCookies(ref httpContext, tokens);
		return new(DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.TokenExpirationInMinutes), HttpStatusCode.Unauthorized);
	}

	private async Task<Result<UserProfile>> LoginAsync(RegisterClient registerClient, Entities.UserProfile user, HttpContext httpContext, CancellationToken ct = default)
	{
		await ExtendCalendar(registerClient, user, ct);
		var claims = await GetUserClaimsAsync(user, ct);
		var tokens = await _jwtProvider.GetTokenPairAsync(claims, user, ct);
		if (tokens is null)
		{
			_logger.LogInformation("Failed to generate tokens for user {Username}", user.Name);
			return new(HttpStatusCode.Unauthorized);
		}
		else
		{
			_logger.LogInformation("Successfully generated tokens for user {Username}", user.Name);
			ConfigureCookies(ref httpContext, tokens);
			return new(user.ToDTO());
		}
	}

	private void ConfigureCookies(ref HttpContext httpContext, TokenResponse tokens)
	{
		httpContext.Response.Cookies.Delete(IAuthService.AccessTokenCookieName);
		httpContext.Response.Cookies.Delete(IAuthService.RefreshTokenCookieName);

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
			new(ClaimTypes.Name, user.FirstName),
			new(ClaimTypes.Surname, user.LastName),
			..roles.Select(r => new Claim(ClaimTypes.Role, r))
		];
		return claims;
	}

	private async Task<Result<UserProfile>> RegisterAsync(RegisterClient registerClient, Entities.School school, HttpContext httpContext, CancellationToken ct = default)
	{
		return await registerClient.GetRoleAsync(ct) switch
		{
			UserRole.Student => await RegisterStudentAsync(registerClient, school, httpContext, ct),
			UserRole.Teacher => await RegisterTeacherAsync(registerClient, school, httpContext, ct),
			_ => new(HttpStatusCode.BadRequest)
		};
	}

	private async Task<Result<UserProfile>> RegisterStudentAsync(RegisterClient registerClient, Entities.School school, HttpContext httpContext, CancellationToken ct = default)
	{
		var registerUserProfile = await registerClient.GetUserProfileAsync(ct);
		if (registerUserProfile is null ||
			registerUserProfile.StudentData is null ||
			registerUserProfile.StudentData.MainClass is null)
		{
			_logger.LogWarning("could not fetch user profile");
			return new(HttpStatusCode.InternalServerError);
		}

		var userProfile = CreateUserProfile(registerUserProfile, school);
		if (userProfile is null ||
			userProfile.Role is not UserRole.Student)
		{
			return new(HttpStatusCode.BadRequest);
		}

		var classroom = await _classroomService.GetOrCreateClassroomAsync(school, registerUserProfile, ct);
		if (classroom is null)
		{
			_logger.LogWarning("Could not get or create classroom");
			return new(HttpStatusCode.InternalServerError);
		}

		var studentProfile = new Entities.StudentProfile
		{
			Classroom = classroom,
			UserProfile = userProfile,
		};

		classroom.Students.Add(studentProfile);
		var userCreateResult = await _userManager.CreateAsync(userProfile);
		if (!userCreateResult.Succeeded)
		{
			_logger.LogWarning("could not create user: {Reason}", userCreateResult.Errors.ToJson());
			return new(userCreateResult.Errors, HttpStatusCode.InternalServerError);
		}

		var role = await EnsureRoleCreatedAsync(userProfile.Role, ct);
		var roleAddedResult = await _userManager.AddToRoleAsync(userProfile, role);
		if (!roleAddedResult.Succeeded)
		{
			return new(roleAddedResult.Errors, HttpStatusCode.InternalServerError);
		}

		return await LoginAsync(registerClient, userProfile, httpContext, ct);
	}

	private async Task<Result<UserProfile>> RegisterTeacherAsync(RegisterClient registerClient, Entities.School school, HttpContext httpContext, CancellationToken ct = default)
	{
		var registerUserProfile = await registerClient.GetUserProfileAsync(ct);
		if (registerUserProfile is null)
		{
			_logger.LogWarning("Unable to fetch user profile");
			return new(HttpStatusCode.InternalServerError);
		}

		var userProfile = CreateUserProfile(registerUserProfile, school);
		if (userProfile is null ||
			userProfile.Role is not UserRole.Teacher)
		{
			return new(HttpStatusCode.BadRequest);
		}

		var userCreateResult = await _userManager.CreateAsync(userProfile);
		if (!userCreateResult.Succeeded)
		{
			_logger.LogWarning("Unable to create user: {Reason}", userCreateResult.Errors.ToJson());
			return new(userCreateResult.Errors, HttpStatusCode.InternalServerError);
		}

		var role = await EnsureRoleCreatedAsync(userProfile.Role, ct);
		var roleAddedResult = await _userManager.AddToRoleAsync(userProfile, role);
		if (!roleAddedResult.Succeeded)
		{
			_logger.LogWarning("Unable to assign roles to user: {Reason}", roleAddedResult.Errors.ToJson());
			return new(roleAddedResult.Errors, HttpStatusCode.InternalServerError);
		}

		// BUG, this might not get updated later on

		var existingTeacherProfile = await _context.Teachers
			.Where(t => t.SchoolId == school.Id && t.FirstName == userProfile.FirstName && t.LastName == userProfile.LastName)
			.FirstOrDefaultAsync(ct);

		var teacherProfile = new Entities.TeacherProfile
		{
			UserProfile = userProfile,
			Teacher = existingTeacherProfile,
		};

		await _context.TeacherProfiles.AddAsync(teacherProfile, ct);

		return await LoginAsync(registerClient, userProfile, httpContext, ct);
	}

	private static Entities.UserProfile? CreateUserProfile(RegisterUserProfile registerUserProfile, Entities.School school)
	{
		var role = RegisterClient.GetRole(registerUserProfile);
		if (role is null) { return null; }
		var guid = Guid.NewGuid();
		return new()
		{
			Id = guid,
			UserName = guid.ToString(),
			RegiserId = registerUserProfile.Id,
			FirstName = registerUserProfile.FirstName,
			LastName = registerUserProfile.LastName,
			Role = (UserRole)role!,
			School = school,
		};
	}

	private async Task<string> EnsureRoleCreatedAsync(UserRole role, CancellationToken ct = default)
	{
		var roleName = role.ToString();
		var existingRole = await _roleManager.FindByNameAsync(roleName).WaitAsync(ct);
		if (existingRole is null)
		{
			await _roleManager.CreateAsync(new(roleName)).WaitAsync(ct);
		}
		return roleName;
	}

	private async Task ExtendCalendar(RegisterClient registerClient, Entities.UserProfile user, CancellationToken ct = default)
	{
		_logger.LogInformation("Extending Calendar for user {User}", user.Name);
		if (user.Role is not UserRole.Student || user.StudentProfile is null || await registerClient.GetRoleAsync(ct) is not UserRole.Student)
		{
			_logger.LogInformation("User is not a student");
			return;
		}

		_context.Entry(user.StudentProfile).Reference(p => p.Classroom).Load();
		_context.Entry(user.StudentProfile.Classroom).Reference(c => c.Calendar).Load();

		var classroom = user.StudentProfile.Classroom;
		if (classroom.Calendar is not null && classroom.Calendar.LastsUntil <= DateTimeOffset.UtcNow)
		{
			_logger.LogInformation("Fetching calendar from DigitalesRegister");
			var calendar = await registerClient.GetCalendarAsync(classroom.Calendar.LastsUntil, DateTimeOffset.UtcNow.AddMonths(1), ct);
			classroom.Calendar.Extend(calendar, user.School, out var createdTeachers, out var createdSubjects);

			_logger.LogInformation("Adding teachers, collection is empty [{isEmpty}]", createdTeachers.Any());
			_logger.LogInformation("Adding Subjects, collection is empty [{isEmpty}]", createdSubjects.Any());

			await _context.Teachers.AddRangeAsync(createdTeachers, ct);
			await _context.Subjects.AddRangeAsync(createdSubjects, ct);
		}
	}
}
