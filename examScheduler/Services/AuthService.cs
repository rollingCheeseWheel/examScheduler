using Entities;
using examScheduler.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Models.API;
using registerClient;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Transactions;
using Util;

namespace examScheduler.Services;

public interface IAuthService
{
	Task<GenericResponse<bool>> RegisterAsync(SignupRequest request, CancellationToken ct);
	Task<GenericResponse<bool>> ResetPasswordAsync(SignupRequest request, CancellationToken ct);
	Task<GenericResponse<bool>> ChangePasswordAsync(UserProfile user, ChangePasswordRequest request, CancellationToken ct);
	Task<GenericResponse<TokenResponse>> LoginAsync(AuthRequest request, CancellationToken ct);
	Task<GenericResponse<TokenResponse>> ExtendTokenAsync(ExtendTokenRequest request, CancellationToken ct);
}

public class AuthService(
	AppDbContext context,
	UserManager<UserProfile> userManager,
	RoleManager<IdentityRole<int>> roleManager,
	IClassroomService classroomService
) : IAuthService
{
	private readonly AppDbContext _context = context;
	private readonly UserManager<UserProfile> _userManager = userManager;
	private readonly RoleManager<IdentityRole<int>> _roleManager = roleManager;
	private readonly IClassroomService classroomService = classroomService;

	private async Task EnsureRoleAsync(string roleName, CancellationToken ct)
	{
		if (!await _roleManager.RoleExistsAsync(roleName))
		{
			await _roleManager.CreateAsync(new IdentityRole<int>(roleName));
		}
	}

	private async Task<UserProfile?> GetUserProfileAsync(AuthRequest request, CancellationToken ct)
	{
		return await _userManager.Users
			.Where(u =>
				u.School.RegisterUri == request.RegisterUri &&
				u.UserName == request.Username
			).FirstOrDefaultAsync(ct);
	}

	private async Task<Entities.School?> GetSchoolAsync(AuthRequest request, CancellationToken ct)
	{
		return await _context.Schools
			.Where(s => s.RegisterUri == request.RegisterUri)
			.FirstOrDefaultAsync(ct);
	}

	private async Task<GenericResponse<UserProfile>> RegisterStudentAsync(RegisterClient client, Entities.School school, CancellationToken ct)
	{
		// get calendar
		var calendar = await client.GetCompleteCalendarAsync(ct);
		if (calendar is null)
		{
			return new("Could not fetch calendar", HttpStatusCode.InternalServerError);
		}
		if (!calendar.TryValidate(out var validationResults))
		{
			return new(validationResults, HttpStatusCode.InternalServerError);
		}

		// ensure classroom (and related teachers/subjects) exists/tracked
		var classroom = await classroomService.GetOrCreateClassroomAsync(school, calendar, ct);
		if (classroom is null)
		{
			return new("Could not determine classroom from calendar", HttpStatusCode.InternalServerError);
		}

		// get register profile for display name
		var registerProfile = await client.GetUserProfileAsync(ct);
		if (registerProfile is null || !registerProfile.TryValidate(out validationResults))
		{
			return new(validationResults ?? [ ], HttpStatusCode.InternalServerError);
		}

		// create identity user (without saving here)
		var user = new UserProfile
		{
			UserName = registerProfile.Username,
			DisplayName = registerProfile.Name,
			School = school
		};

		// create student profile and link to classroom (tracked by EF)
		var student = new StudentProfile
		{
			UserProfile = user,
			Classroom = classroom
		};

		// track student (and classroom if new) in the DbContext; persisting happens in outer flow
		_context.StudentProfiles.Add(student);
		classroom.AddStudent(student);

		return new(user, HttpStatusCode.OK);
	}

	private async Task<GenericResponse<UserProfile>> RegisterTeacherAsync(RegisterClient client, Entities.School school, CancellationToken ct)
	{
		throw new NotImplementedException();
	}

	public async Task<GenericResponse<bool>> RegisterAsync(SignupRequest request, CancellationToken ct)
	{
		ICollection<ValidationResult>? validationResults;

		var school = await GetSchoolAsync(request, ct);
		if (school is null)
		{
			return new("Unknown school uri");
		}

		// check if registered
		var alreadyExists = await GetUserProfileAsync(request, ct);
		if (alreadyExists is not null)
		{
			return new("Account already exists", HttpStatusCode.Conflict);
		}

		// validate credentials
		using var client = new RegisterClient(request);
		if (!await client.ValidateCredentials(ct))
		{
			return new("Invalid credentials");
		}

		// get userprofile
		var userProfile = await client.GetUserProfileAsync(ct);
		if (userProfile is null)
		{
			return new("Could not fetch user profile", HttpStatusCode.InternalServerError);
		}
		if (!userProfile.TryValidate(out validationResults))
		{
			return new(validationResults, HttpStatusCode.InternalServerError);
		}

		// get role
		using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
		var registerProfileRole = IRegisterClient.GetUserRole(userProfile);
		GenericResponse<UserProfile> newUserGenericRequest = registerProfileRole switch
		{
			UserProfileRoles.Student => await RegisterStudentAsync(client, school, ct),
			UserProfileRoles.Teacher => await RegisterTeacherAsync(client, school, ct),
			_ => new(HttpStatusCode.BadRequest)
		};

		var userRoleName = registerProfileRole switch
		{
			UserProfileRoles.Student => RoleNames.Student,
			UserProfileRoles.Teacher => RoleNames.Teacher,
			_ => null
		};

		if (!newUserGenericRequest.Success || userRoleName is null)
		{
			return new(newUserGenericRequest.Errors ?? [ ], newUserGenericRequest.StatusCode);
		}

		var userCreatedResult = await _userManager.CreateAsync(newUserGenericRequest.Result!, request.NewPassword);
		if (!userCreatedResult.Succeeded)
		{
			return new(userCreatedResult);
		}

		await EnsureRoleAsync(userRoleName, ct);
		var userRoleResult = await _userManager.AddToRoleAsync(newUserGenericRequest.Result!, userRoleName);
		if (!userRoleResult.Succeeded)
		{
			return new(userRoleResult);
		}

		transaction.Complete();
		await _context.SaveChangesAsync(ct);
		return new(true);
	}

	public async Task<GenericResponse<bool>> ResetPasswordAsync(SignupRequest request, CancellationToken ct)
	{
		var user = await GetUserProfileAsync(request, ct);
		if (user is null)
		{
			return new("Account does not exist", HttpStatusCode.NotFound);
		}

		// check if credentials are valid
		using var registerClient = new RegisterClient(request);
		if (!await registerClient.ValidateCredentials(ct))
		{
			return new("Invalid credentials");
		}

		var userProfile = await registerClient.GetUserProfileAsync(ct);
		if (userProfile is null)
		{
			return new("Could not fetch user profile", HttpStatusCode.InternalServerError);
		}
		if (!userProfile.TryValidate(out var validationResults))
		{
			return new(validationResults, HttpStatusCode.InternalServerError);
		}

		// check if information matches
		if (!user.MatchesRegisterProfile(userProfile))
		{
			return new("User profiles do not match", HttpStatusCode.InternalServerError);
		}

		using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

		var token = await _userManager.GeneratePasswordResetTokenAsync(user);
		var userResult = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

		if (!userResult.Succeeded)
		{
			return new(userResult.Errors);
		}

		transaction.Complete();

		return new(true, HttpStatusCode.Created);
	}

	public async Task<GenericResponse<bool>> ChangePasswordAsync(UserProfile user, ChangePasswordRequest request, CancellationToken ct)
	{
		if (!request.TryValidate(out var validationResults))
		{
			return new(validationResults);
		}

		using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

		var changeResult = await _userManager.ChangePasswordAsync(user, request.NewPassword, request.OldPassword);
		if (!changeResult.Succeeded)
		{
			return new(changeResult.Errors);
		}

		transaction.Complete();

		return new(true, HttpStatusCode.Created);
	}

	public Task<GenericResponse<TokenResponse>> LoginAsync(AuthRequest request, CancellationToken ct)
	{
		throw new NotImplementedException();
	}

	public Task<GenericResponse<TokenResponse>> ExtendTokenAsync(ExtendTokenRequest request, CancellationToken ct)
	{
		throw new NotImplementedException();
	}
}
