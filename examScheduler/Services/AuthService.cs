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
		using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transcation = await _context.Database.BeginTransactionAsync(ct);

		// verify that the school exists
		Entities.School? school = await _context.Schools.FindAsync([ request.SchoolId ], ct);
		if (school is null)
		{
			return new(HttpStatusCode.BadRequest, "Unknown School ID");
		}

		// create the registerClient and fetch the userprofile
		using var registerClient = new RegisterClient(school, request.AuthCode);
		RegisterUserProfile? userProfile = await registerClient.GetUserProfileAsync(ct);
		if (userProfile is null)
		{
			return new(HttpStatusCode.InternalServerError, "Could not fetch user profile");
		}

		Entities.UserProfile? existingUser = await _userManager.Users
			.FirstOrDefaultAsync(u => u.SchoolId == school.Id && u.RegiserId == userProfile.Id, ct);
		Result<UserProfile>? response = null;
		if (existingUser is not null) // dbUser found 
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

			Entities.UserProfile? justCreatedUser = await _userManager.Users.FirstOrDefaultAsync(u => u.SchoolId == school.Id && u.RegiserId == userProfile.Id, ct);
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
		Entities.RefreshTokenSession? token = await _context.RefreshSessions.FirstOrDefaultAsync(s => s.TokenValue == refreshToken, ct);
		if (token is null) { return new(HttpStatusCode.NotFound); }
		Entities.UserProfile? user = await _context.Users.FindAsync([ token.UserProfileId ], ct);
		if (user is null) { return new(HttpStatusCode.NotFound); }
		ICollection<Claim> claims = await GetUserClaimsAsync(user, ct);
		TokenResponse? tokens = await _jwtProvider.RefreshTokenPairAsync(claims, refreshToken, user, ct);
		ConfigureCookies(ref httpContext, tokens);
		return new(
			DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.TokenExpirationInMinutes),
			HttpStatusCode.Unauthorized,
			tokens is not null
		);
	}

	private async Task<Result<UserProfile>> LoginAsync(Entities.UserProfile user, HttpContext httpContext, CancellationToken ct = default)
	{
		ICollection<Claim> claims = await GetUserClaimsAsync(user, ct);
		TokenResponse? tokens = await _jwtProvider.GetTokenPairAsync(claims, user, ct);
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
		IList<string> roles = await _userManager.GetRolesAsync(user).WaitAsync(ct);

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
		UserRole.Student => await RegisterStudentAsync(registerClient, school, httpContext, ct),
		UserRole.Teacher => await RegisterTeacherAsync(registerClient, school, httpContext, ct),
		_ => new(HttpStatusCode.BadRequest)
	};

	private async Task<Result<UserProfile>> RegisterStudentAsync(RegisterClient registerClient, Entities.School school, HttpContext httpContext, CancellationToken ct = default)
	{
		RegisterUserProfile? registerUserProfile = await registerClient.GetUserProfileAsync(ct);
		if (registerUserProfile is null ||
			registerUserProfile.StudentData is null ||
			registerUserProfile.StudentData.MainClass is null)
		{
			_logger.LogWarning("could not fetch user profile");
			return new(HttpStatusCode.InternalServerError);
		}

		Entities.UserProfile? userProfile = CreateUserProfile(registerUserProfile, school);
		if (userProfile is null ||
			userProfile.Role is not UserRole.Student)
		{
			return new(HttpStatusCode.BadRequest);
		}

		Entities.Classroom? classroom = await _classroomService.GetOrCreateClassroomAsync(school, registerUserProfile, ct);
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
		IdentityResult userCreateResult = await _userManager.CreateAsync(userProfile);
		if (!userCreateResult.Succeeded)
		{
			_logger.LogWarning("could not create user: {Reason}", userCreateResult.Errors.ToJson());
			return new(HttpStatusCode.InternalServerError, userCreateResult.Errors);
		}

		var role = await EnsureRoleCreatedAsync(userProfile.Role, ct);
		IdentityResult roleAddedResult = await _userManager.AddToRoleAsync(userProfile, role);
		return !roleAddedResult.Succeeded
			? new(HttpStatusCode.InternalServerError, roleAddedResult.Errors)
			: await LoginAsync(userProfile, httpContext, ct);
	}

	private async Task<Result<UserProfile>> RegisterTeacherAsync(RegisterClient registerClient, Entities.School school, HttpContext httpContext, CancellationToken ct = default)
	{
		RegisterUserProfile? registerUserProfile = await registerClient.GetUserProfileAsync(ct);
		if (registerUserProfile is null)
		{
			_logger.LogWarning("Unable to fetch user profile");
			return new(HttpStatusCode.InternalServerError);
		}

		Entities.UserProfile? userProfile = CreateUserProfile(registerUserProfile, school);
		if (userProfile is null ||
			userProfile.Role is not UserRole.Teacher)
		{
			return new(HttpStatusCode.BadRequest);
		}

		IdentityResult userCreateResult = await _userManager.CreateAsync(userProfile);
		if (!userCreateResult.Succeeded)
		{
			_logger.LogWarning("Unable to create user: {Reason}", userCreateResult.Errors.ToJson());
			return new(HttpStatusCode.InternalServerError, userCreateResult.Errors);
		}

		var role = await EnsureRoleCreatedAsync(userProfile.Role, ct);
		IdentityResult roleAddedResult = await _userManager.AddToRoleAsync(userProfile, role);
		if (!roleAddedResult.Succeeded)
		{
			_logger.LogWarning("Unable to assign roles to user: {Reason}", roleAddedResult.Errors.ToJson());
			return new(HttpStatusCode.InternalServerError, roleAddedResult.Errors);
		}

		// BUG, this might not get updated later on

		Entities.Teacher? existingTeacherProfile = await _context.Teachers
			.Where(t => t.SchoolId == school.Id && t.FirstName == userProfile.FirstName && t.LastName == userProfile.LastName)
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
		UserRole? role = RegisterClient.GetRole(registerUserProfile);
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
		IdentityRole<Guid>? existingRole = await _roleManager.FindByNameAsync(roleName).WaitAsync(ct);
		if (existingRole is null)
		{
			await _roleManager.CreateAsync(new(roleName)).WaitAsync(ct);
		}
		return roleName;
	}

	private async Task ExtendCalendar(RegisterClient registerClient, Entities.UserProfile user, CancellationToken ct = default)
	{
		if (user.Role is not UserRole.Student || user.StudentProfile is null || await registerClient.GetRoleAsync(ct) is not UserRole.Student)
		{
			_logger.LogInformation("User is not a student");
			return;
		}

		_logger.LogInformation("Enqueuing task in worker");
		await _calendarWorker.EnqueueAsync(async (serviceProvider, logger, ct) =>
		{
			await Task.Delay(1000, ct);
			using AppDbContext dbcontext = serviceProvider.ServiceProvider.GetRequiredService<AppDbContext>();
			using IRegisterClient rgClient = registerClient.Copy();

			Entities.UserProfile? dbUser = await dbcontext.Users.FindAsync([ user.Id ], ct);

			if (dbUser is null || dbUser.StudentProfile is null || dbUser.Role is not UserRole.Student)
			{
				logger.LogWarning("Unable to fetch user from DB, user does not have a studentprofile or is not a student");
				return;
			}

			await dbcontext.Entry(dbUser.StudentProfile).Reference(p => p.Classroom).LoadAsync(ct);
			await dbcontext.Entry(dbUser.StudentProfile.Classroom).Reference(c => c.Calendar).LoadAsync(ct);

			if (dbUser.StudentProfile.Classroom.Calendar is null)
			{
				logger.LogWarning("Calendar of student is null");
				return;
			}

			IEnumerable<Models.DigitalesRegister.Lesson> registerCalendar = await rgClient.GetCalendarAsync(dbUser.StudentProfile.Classroom.Calendar.LastsUntil, DateTimeOffset.UtcNow.AddMonths(1), ct);
			dbUser.StudentProfile.Classroom.Calendar.Extend(registerCalendar, dbUser.School, out IEnumerable<Entities.Teacher>? createdTeachers, out IEnumerable<Entities.Subject>? createdSubjects);

			await dbcontext.Teachers.AddRangeAsync(createdTeachers, ct);
			await dbcontext.Subjects.AddRangeAsync(createdSubjects, ct);

			await dbcontext.SaveChangesAsync(ct);
		}, ct);
	}
}
