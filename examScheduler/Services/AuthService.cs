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
using Util;

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
	ITokenProvider jwtProvider,
	ILogger<AuthService> logger
) : IAuthService
{
	private readonly AppDbContext _context = context;
	private readonly UserManager<UserProfile> _userManager = userManager;
	private readonly RoleManager<IdentityRole<Guid>> _roleManager = roleManager;
	private readonly IClassroomService _classroomService = classroomService;
	private readonly ITokenProvider _jwtProvider = jwtProvider;
	private readonly ILogger _logger = logger;

	private void Log(string message, params object[ ] args) => _logger.LogInformation(message, args);
	private void Log(string message, object obj) => _logger.LogInformation($"{message} - {obj.ToJson()}");

	public async Task<Result<TokenResponse>> AuthenticateAsync(OAuthRequest request, CancellationToken ct = default)
	{
		//Log($"received request {DateTime.UtcNow}");
		using var transcation = await _context.Database.BeginTransactionAsync(ct);

		// verify that the school exists
		var school = await _context.Schools
			.FirstOrDefaultAsync(s => s.SchoolId == request.SchoolId, ct);
		if (school is null)
		{
			return new("Unknown School ID", HttpStatusCode.BadRequest);
		}
		//Log("School", school.Name);

		// create the registerClient and fetch the userprofile
		using var registerClient = new RegisterClient(school, request.AuthCode);
		var userProfile = await registerClient.GetUserProfileAsync(ct);
		if (userProfile is null)
		{
			return new("Could not fetch user profile", HttpStatusCode.InternalServerError);
		}

		//Log("Userprofile", userProfile);

		var existingUser = await _userManager.Users
			.Include(u => u.StudentProfile)
			.Include(u => u.TeacherProfile)
			.FirstOrDefaultAsync(u => u.SchoolId == school.Id && u.UserName == userProfile.Id.ToString(), ct);
		Result<TokenResponse>? response = null;
		if (existingUser is not null) // user found 
		{
			//Log("Logging user in");
			response = await LoginAsync(registerClient, existingUser, ct);
		}
		else
		{
			//Log("registering user");
			response = await RegisterAsync(registerClient, school, ct);
		}

		await _context.SaveChangesAsync(ct);
		if (response.Success)
		{
			//Log("Successfully authenticated");
			await transcation.CommitAsync(ct);
		}
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

	private async Task<Result<TokenResponse>> LoginAsync(RegisterClient registerClient, UserProfile user, CancellationToken ct = default)
	{
		//Log("Logging in");
		await ExtendCalendar(registerClient, user, ct);
		var claims = await GetUserClaimsAsync(user, ct);
		//Log("claims", claims);
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

	private async Task<Result<TokenResponse>> RegisterAsync(RegisterClient registerClient, Entities.School school, CancellationToken ct = default)
	{
		return await registerClient.GetRoleAsync(ct) switch
		{
			UserProfileRole.Student => await RegisterStudentAsync(registerClient, school, ct),
			UserProfileRole.Teacher => await RegisterTeacherAsync(registerClient, school, ct),
			_ => new(HttpStatusCode.BadRequest)
		};
	}

	private async Task<Result<TokenResponse>> RegisterStudentAsync(RegisterClient registerClient, Entities.School school, CancellationToken ct = default)
	{
		//Log("Registering student");
		var registerUserProfile = await registerClient.GetUserProfileAsync(ct);
		if (registerUserProfile is null || // could not fetch
			registerUserProfile.StudentData is null || // doesnt have student data
			registerUserProfile.StudentData.MainClass is null) // doesnt have a main class asigned
		{
			//Log("could not fetch registerUserProfile");
			return new(HttpStatusCode.InternalServerError);
		}

		var userProfile = await CreateUserProfileAsync(registerUserProfile, school, ct);
		if (userProfile is null ||
			userProfile.Role is not UserProfileRole.Student)
		{
			//Log("could not create userProfile");
			return new(HttpStatusCode.BadRequest);
		}

		var classroom = await _classroomService.GetOrCreateClassroomAsync(school, registerUserProfile, ct);
		if (classroom is null)
		{
			//Log("Could not get or create classroom");
			return new(HttpStatusCode.InternalServerError);
		}

		var studentProfile = new StudentProfile
		{
			Classroom = classroom,
			UserProfile = userProfile,
		};

		//Log("StudentProfile", studentProfile);

		classroom.Students.Add(studentProfile);
		var userCreateResult = await _userManager.CreateAsync(userProfile);
		if (!userCreateResult.Succeeded)
		{
			return new(userCreateResult.Errors, HttpStatusCode.InternalServerError);
		}

		var role = await EnsureRoleCreatedAsync(userProfile.Role, ct);
		//Log("role", role);
		var roleAddedResult = await _userManager.AddToRoleAsync(userProfile, role);
		if (!roleAddedResult.Succeeded)
		{
			return new(roleAddedResult.Errors, HttpStatusCode.InternalServerError);
		}

		return await LoginAsync(registerClient, userProfile, ct);
	}

	private async Task<Result<TokenResponse>> RegisterTeacherAsync(RegisterClient registerClient, Entities.School school, CancellationToken ct = default)
	{
		var registerUserProfile = await registerClient.GetUserProfileAsync(ct);
		if (registerUserProfile is null)
		{
			return new(HttpStatusCode.InternalServerError);
		}

		var userProfile = await CreateUserProfileAsync(registerUserProfile, school, ct);
		if (userProfile is null ||
			userProfile.Role is not UserProfileRole.Teacher)
		{
			return new(HttpStatusCode.BadRequest);
		}

		var userCreateResult = await _userManager.CreateAsync(userProfile);
		if (!userCreateResult.Succeeded)
		{
			return new(userCreateResult.Errors, HttpStatusCode.InternalServerError);
		}

		var role = await EnsureRoleCreatedAsync(userProfile.Role, ct);
		var roleAddedResult = await _userManager.AddToRoleAsync(userProfile, role);
		if (!roleAddedResult.Succeeded)
		{
			return new(roleAddedResult.Errors, HttpStatusCode.InternalServerError);
		}

		var existingTeacherProfile = await _context.Teachers
			.Where(t => t.SchoolId == school.Id && t.FirstName == userProfile.FirstName && t.LastName == userProfile.LastName)
			.FirstOrDefaultAsync(ct);

		var teacherProfile = new TeacherProfile
		{
			UserProfile = userProfile,
			Teacher = existingTeacherProfile,
		};

		await _context.TeacherProfiles.AddAsync(teacherProfile);

		return await LoginAsync(registerClient, userProfile, ct);
	}

	private async Task<UserProfile?> CreateUserProfileAsync(RegisterUserProfile registerUserProfile, Entities.School school, CancellationToken ct = default)
	{
		var role = RegisterClient.GetRole(registerUserProfile);
		if (role is null) { return null; }
		var guid = Guid.NewGuid();
		var userProfile = new UserProfile
		{
			Id = guid,
			UserName = guid.ToString(),
			RegiserId = registerUserProfile.Id,
			FirstName = registerUserProfile.FirstName,
			LastName = registerUserProfile.LastName,
			Role = (UserProfileRole)role!,
			School = school,
		};
		return userProfile;
	}

	private async Task<string> EnsureRoleCreatedAsync(UserProfileRole role, CancellationToken ct = default)
	{
		var roleName = role.ToString();
		var existingRole = await _roleManager.FindByNameAsync(roleName).WaitAsync(ct);
		if (existingRole is null)
		{
			await _roleManager.CreateAsync(new(roleName)).WaitAsync(ct);
		}
		return roleName;
	}

	private async Task ExtendCalendar(RegisterClient registerClient, UserProfile user, CancellationToken ct = default)
	{
		if (user.Role is not UserProfileRole.Student || user.StudentProfile is null) { return; }

		var userProfile = await registerClient.GetRoleAsync(ct);
		if (userProfile is not UserProfileRole.Student) { return; }

		_context.Entry(user.StudentProfile).Reference(p => p.Classroom).Load();
		_context.Entry(user.StudentProfile.Classroom).Reference(c => c.Calendar).Load();

		var classroom = user.StudentProfile.Classroom;
		if (classroom.Calendar is not null && classroom.Calendar.LastsUntil <= DateTimeOffset.UtcNow)
		{
			var calendar = await registerClient.GetCalendarAsync(classroom.Calendar.LastsUntil, DateTimeOffset.UtcNow.AddMonths(1), ct);
			classroom.Calendar.Extend(calendar, user.School, out var createdTeachers, out var createdSubjects);

			await _context.Teachers.AddRangeAsync(createdTeachers, ct);
			await _context.Subjects.AddRangeAsync(createdSubjects, ct);
		}
	}
}
