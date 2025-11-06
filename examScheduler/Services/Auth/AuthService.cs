using Entities;
using examScheduler.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Models.API;
using Models.DigitalesRegister;
using registerClient;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace examScheduler.Services.Auth;

public interface IAuthService
{
	Task<GenericResponse<RegisterProfileModel>> RegisterAsync(SignupRequest request, CancellationToken ct);
	Task<GenericResponse<RegisterProfileModel>> ChangePasswordAsync(SignupRequest request, CancellationToken ct);
	Task<GenericResponse<TokenResponse>> LoginAsync(AuthRequest request, CancellationToken ct);
	Task<GenericResponse<TokenResponse>> ExtendTokenAsync(ExtendTokenRequest request, CancellationToken ct);
}

public class AuthService(AppDbContext context) : IAuthService
{
	private readonly AppDbContext _context = context;

	private async Task<UserProfile?> TryGetUserprofile(AuthRequest request, CancellationToken ct)
	{
		var temp = _context.UserProfiles.Where(u =>
			u.School.RegisterUri == request.RegisterUri &&
			u.UserName == request.Username);

		return temp.Any()
			? await temp.FirstAsync(ct)
			: null;
	}

	private async Task<bool> DoesUserExists(AuthRequest request, CancellationToken ct) => await TryGetUserprofile(request, ct) is not null;

	public async Task<GenericResponse<RegisterProfileModel>> RegisterAsync(SignupRequest request, CancellationToken ct)
	{
		// check if registered
		var alreadyExists = await TryGetUserprofile(request, ct);
		if (await DoesUserExists(request, ct))
		{
			return new("Account already exists");
		}

		throw new NotImplementedException();
	}

	public async Task<GenericResponse<RegisterProfileModel>> ChangePasswordAsync(SignupRequest request, CancellationToken ct)
	{
		// check if registered
		var dbUserProfile = await TryGetUserprofile(request, ct);
		if (dbUserProfile is null)
		{
			return new("Account does not exist");
		}

		// check if credentials are valid
		using var registerClient = new RegisterClient(request);
		var userProfile = await registerClient.GetUserProfileAsync(ct);
		if (userProfile is null)
		{
			return new("Invalid credentials");
		}

		// check if information matches
		if (!dbUserProfile.MatchesRegisterProfile(userProfile))
		{
			return new("User profiles do not match");
		}

		

		throw new NotImplementedException();
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
