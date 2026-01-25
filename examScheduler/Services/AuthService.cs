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
	ILogger<AuthService> logger,
	ICalendarWorker calendarWorker
) : IAuthService
{
	private readonly AppDbContext _context = context;
	private readonly UserManager<Entities.UserProfile> _userManager = userManager;
	private readonly RoleManager<IdentityRole<Guid>> _roleManager = roleManager;
	private readonly IClassroomService _classroomService = classroomService;
	private readonly ITokenProvider _jwtProvider = jwtProvider;
	private readonly JwtOptions _jwtOptions = jwtOptions;
	private readonly ILogger _logger = logger;
	private readonly ICalendarWorker _calendarWorker = calendarWorker;

	public async Task<Result<UserProfile>> AuthenticateAsync(OAuthRequest request, HttpContext httpContext, CancellationToken ct = default)
	{
		using var transcation = await _context.Database.BeginTransactionAsync(ct);

		var school = await _context.Schools.AsNoTracking().FirstOrDefaultAsync(s => s.SchoolId == request.SchoolId, ct);
		if (school is null)
		{
			return new(HttpStatusCode.BadRequest, "Unknown School ID");
		}

		using var registerClient = new RegisterClient(school, request.AuthCode);
		var userProfile = await registerClient.GetUserProfileAsync(ct);
		if (userProfile is null)
		{
			return new(HttpStatusCode.InternalServerError, "Could not fetch user profile");
		}

		var existingUser = await _userManager.Users
			.FirstOrDefaultAsync(u => u.SchoolId == school.Id && u.RegiserId == userProfile.Id, ct);
		Result<UserProfile>? response = null;
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

			var justCreatedUser = await _userManager.Users.FirstOrDefaultAsync(u => u.SchoolId == school.Id && u.RegiserId == userProfile.Id, ct);
			if (justCreatedUser is not null)
			{
				await ExtendCalendar(registerClient, justCreatedUser, ct);
			}
		}
		else
		{
			_logger.LogWarning("Login unsuccessful: {Reason}", response.Errors.ToJson());
			await transcation.RollbackAsync(ct);
		}
		return response;
	}

	public async Task<Result<DateTimeOffset>> RefreshTokenAsync(string refreshToken, HttpContext httpContext, CancellationToken ct = default)
	{
		var token = await _context.RefreshSessions.FirstOrDefaultAsync(s => s.TokenValue == refreshToken, ct);
		if (token is null) { return new(HttpStatusCode.NotFound); }
		var user = await _context.Users.FindAsync([ token.UserProfileId ], ct);
		if (user is null) { return new(HttpStatusCode.NotFound); }
		var claims = await GetUserClaimsAsync(user, ct);
		var tokens = await _jwtProvider.RefreshTokenPairAsync(claims, refreshToken, user, ct);
		ConfigureCookies(ref httpContext, tokens);
		return new(
			DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.TokenExpirationInMinutes),
			HttpStatusCode.Unauthorized,
			tokens is not null
		);
	}

	private async Task<Result<UserProfile>> LoginAsync(Entities.UserProfile user, HttpContext httpContext, CancellationToken ct = default)
	{
		if (user.TeacherProfile is not null)
		{
			await ConnectTeacherWithCalendarTeacherAsync(user.Id, user.SchoolId, ct);
		}

		var claims = await GetUserClaimsAsync(user, ct);
		var tokens = await _jwtProvider.GetTokenPairAsync(claims, user, ct);
		if (tokens is null)
		{
			_logger.LogInformation("Failed to generate tokens for user {Username}", user.Name);
		}
		else
		{
			_logger.LogInformation("Successfully generated tokens for user {Username}", user.Name);
			ConfigureCookies(ref httpContext, tokens);
		}
		return new(user.ToDTO(), HttpStatusCode.Unauthorized, tokens is not null);
	}

	private void ConfigureCookies(ref HttpContext httpContext, TokenResponse? tokens)
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

	private async Task<Result<UserProfile>> RegisterAsync(RegisterClient registerClient, Entities.School school, HttpContext httpContext, CancellationToken ct = default) => await registerClient.GetRoleAsync(ct) switch
	{
		UserRoles.Student => await RegisterStudentAsync(registerClient, school, httpContext, ct),
		UserRoles.Teacher => await RegisterTeacherAsync(registerClient, school, httpContext, ct),
		_ => new(HttpStatusCode.BadRequest)
	};

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
			userProfile.Role is not UserRoles.Student)
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
			return new(HttpStatusCode.InternalServerError, userCreateResult.Errors);
		}

		var role = await EnsureRoleCreatedAsync(userProfile.Role, ct);
		var roleAddedResult = await _userManager.AddToRoleAsync(userProfile, role);
		return !roleAddedResult.Succeeded
			? new(HttpStatusCode.InternalServerError, roleAddedResult.Errors)
			: await LoginAsync(userProfile, httpContext, ct);
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
			userProfile.Role is not UserRoles.Teacher)
		{
			return new(HttpStatusCode.BadRequest);
		}

		var userCreateResult = await _userManager.CreateAsync(userProfile);
		if (!userCreateResult.Succeeded)
		{
			_logger.LogWarning("Unable to create user: {Reason}", userCreateResult.Errors.ToJson());
			return new(HttpStatusCode.InternalServerError, userCreateResult.Errors);
		}

		var role = await EnsureRoleCreatedAsync(userProfile.Role, ct);
		var roleAddedResult = await _userManager.AddToRoleAsync(userProfile, role);
		if (!roleAddedResult.Succeeded)
		{
			_logger.LogWarning("Unable to assign roles to user: {Reason}", roleAddedResult.Errors.ToJson());
			return new(HttpStatusCode.InternalServerError, roleAddedResult.Errors);
		}

		// on each login the connection will be tried to be established
		var existingTeacherProfile = await _context.Teachers
			.Where(t => t.SchoolId == school.Id)
			.Where(t => t.FirstName == userProfile.FirstName && t.LastName == userProfile.LastName)
			.FirstOrDefaultAsync(ct);

		var teacherProfile = new Entities.TeacherProfile
		{
			UserProfile = userProfile,
			Teacher = existingTeacherProfile,
		};

		await _context.TeacherProfiles.AddAsync(teacherProfile, ct);

		return await LoginAsync(userProfile, httpContext, ct);
	}

	private static Entities.UserProfile? CreateUserProfile(RegisterUserProfile registerUserProfile, Entities.School school)
	{
		var role = RegisterClient.GetRole(registerUserProfile);
		if (role is null || Enum.IsDefined(typeof(UserRoles), role))
		{
			return null;
		}
		var guid = Guid.NewGuid();
		return new()
		{
			Id = guid,
			UserName = guid.ToString(),
			RegiserId = registerUserProfile.Id,
			FirstName = registerUserProfile.FirstName,
			LastName = registerUserProfile.LastName,
			Role = (UserRoles)role!,
			SchoolId = school.Id,
		};
	}

	private async Task<string> EnsureRoleCreatedAsync(UserRoles role, CancellationToken ct = default)
	{
		var roleName = role.ToString();
		var existingRole = await _roleManager.FindByNameAsync(roleName).WaitAsync(ct);
		if (existingRole is null)
		{
			await _roleManager.CreateAsync(new(roleName)).WaitAsync(ct);
		}
		return roleName;
	}

	private async Task ConnectTeacherWithCalendarTeacherAsync(Guid teacherId, Guid schoolId, CancellationToken ct = default)
	{
		var teacherProfile = await _context.TeacherProfiles.FindAsync([ teacherId ], ct);
		if (teacherProfile is null) { return; }

		var teacher = await _context.Teachers
			.Where(t => t.SchoolId == teacherProfile.UserProfile.SchoolId)
			.Where(t => t.Name == teacherProfile.UserProfile.Name)
			.FirstOrDefaultAsync(ct);

		if (teacher is null) { return; }

		teacherProfile.Teacher = teacher;
	}

	private async Task ExtendCalendar(RegisterClient registerClient, Entities.UserProfile user, CancellationToken ct = default)
	{
		if (user.Role is not UserRoles.Student || user.StudentProfile is null || await registerClient.GetRoleAsync(ct) is not UserRoles.Student)
		{
			_logger.LogInformation("User is not a student");
			return;
		}

		_logger.LogInformation("Enqueuing task in worker");
		await _calendarWorker.EnqueueAsync(async (serviceProvider, logger, ct) =>
		{
			await Task.Delay(1000, ct);
			using var dbcontext = serviceProvider.ServiceProvider.GetRequiredService<AppDbContext>();
			var calendarService = serviceProvider.ServiceProvider.GetRequiredService<ICalendarService>();
			using var copiedRegisterClient = registerClient.Copy();

			var student = await dbcontext.StudentProfiles.AsNoTracking().FindByIdAsync(user.Id, ct);
			if (student is null)
			{
				logger.LogWarning("Unable to fetch user from DB, user does not have a studentprofile or is not a student");
				return;
			}

			var classroom = await dbcontext.Classrooms.AsNoTracking().FindByIdAsync(student.ClassroomId, ct);
			if (classroom is null || classroom.Calendar is null)
			{
				logger.LogWarning("Calendar or classroom is null");
				return;
			}

			var digitalRegisterLessons = await copiedRegisterClient.GetCalendarAsync(classroom.Calendar.LastsUntil, DateTimeOffset.UtcNow.AddMonths(1), ct);
			if (digitalRegisterLessons is null || !digitalRegisterLessons.Any())
			{
				logger.LogWarning("Unable to fetch calendar from digital register for {student}", student.UserProfile.Name);
				return;
			}
			var success = await calendarService.TryExtendCalendar(classroom.Calendar.Id, student.UserProfile.SchoolId, digitalRegisterLessons, ct);
			if (success)
			{
				logger.LogInformation("Successfully extended calendar for {student}", student.UserProfile.Name);
			}
			else
			{
				logger.LogWarning("Failed to extend calendar for {student}", student.UserProfile.Name);
			}
		}, ct);
	}
}
