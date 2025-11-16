using Entities;
using examScheduler.Data;
using Microsoft.AspNetCore.Identity;
using Models.API;
using registerClient;
using System.Text.RegularExpressions;

namespace examScheduler.Services;

public interface IAuthService
{
	Task<GenericResponse<TokenResponse>> AuthenticateAsync(OAuthRequest request, CancellationToken ct);
	Task<GenericResponse<TokenResponse>> ExtendTokenAsync(TokenExtendRequest request, CancellationToken ct);
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

	public async Task<GenericResponse<TokenResponse>> AuthenticateAsync(OAuthRequest request, CancellationToken ct)
	{
		// verify that the school exists
		if (!_context.Schools.Any(s => s.SchoolId == request.SchoolId))
		{
			return new("Unknown School ID");
		}

		


		using var client = new RegisterClient();

	}

	public Task<GenericResponse<TokenResponse>> ExtendTokenAsync(TokenExtendRequest request, CancellationToken ct)
	{
		throw new NotImplementedException();
	}
}
