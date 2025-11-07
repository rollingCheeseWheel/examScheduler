using Entities;
using examScheduler.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Models.API;
using Models.DigitalesRegister;
using registerClient;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Net;
using System.ComponentModel.DataAnnotations;
using Util;
using Microsoft.OpenApi.Validations;
using System.Transactions;

namespace examScheduler.Services;

public interface IAuthService
{
	Task<GenericResponse<bool>> RegisterAsync(SignupRequest request, CancellationToken ct);
	Task<GenericResponse<bool>> ChangePasswordAsync(SignupRequest request, CancellationToken ct);
	Task<GenericResponse<TokenResponse>> LoginAsync(AuthRequest request, CancellationToken ct);
	Task<GenericResponse<TokenResponse>> ExtendTokenAsync(ExtendTokenRequest request, CancellationToken ct);
}

public class AuthService(AppDbContext context, UserManager<UserProfile> manager) : IAuthService
{
	private readonly AppDbContext _context = context;
	private readonly UserManager<UserProfile> _userManager = manager;

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

	public async Task<GenericResponse<bool>> RegisterAsync(SignupRequest request, CancellationToken ct)
	{
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

		using var client = new RegisterClient(request);
		if (!await client.ValidateCredentials(ct))
		{
			return new("Invalid credentials");
		}

		// check userprofile
		var userProfile = await client.GetUserProfileAsync(ct);
		if (userProfile is null)
		{
			return new("Could not fetch user profile", HttpStatusCode.InternalServerError);
		}
		if (!userProfile.TryValidate(out var validationResults))
		{
			return new(validationResults, HttpStatusCode.InternalServerError);
		}

		var calendar = await client.GetCompleteCalendarAsync(ct);
		if (calendar is null)
		{
			return new("Could not fetch calendar", HttpStatusCode.InternalServerError);
		}
		if (!calendar.TryValidate(out var results))
		{
			return new(results, HttpStatusCode.InternalServerError);
		}

		var userRole = IRegisterClient.GetUserRole(userProfile);

		if (userRole is UserProfileRoles.Unknown)
		{
			return new("Unknown account type", HttpStatusCode.InternalServerError);
		}

		var newUser = new UserProfile
		{
			School = school,
			UserName = request.Username,
			DisplayName = userProfile.Name,

		};

		using var transaction = new TransactionScope();

		var userCreatedResult = await _userManager.CreateAsync(newUser, request.NewPassword);

		if (!userCreatedResult.Succeeded)
		{

		}

		transaction.Complete();

		throw new NotImplementedException();
	}

	public async Task<GenericResponse<bool>> ChangePasswordAsync(SignupRequest request, CancellationToken ct)
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

		using var transaction = new TransactionScope();

		var token = await _userManager.GeneratePasswordResetTokenAsync(user);
		var userResult = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

		if (!userResult.Succeeded)
		{
			return new(userResult.Errors);
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
