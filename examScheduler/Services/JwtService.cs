using Entities;
using examScheduler.Data;
using Microsoft.IdentityModel.Tokens;
using Models.API;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace examScheduler.Services;

public interface IJwtService
{
	public Task<TokenResponse> GetTokensAsync(ICollection<Claim> claims, UserProfile user, CancellationToken ct);
	public string GetAccessToken(ICollection<Claim> claims, int expiresInMinutes);
	public Task<string> GetExtensionTokenAsync(UserProfile user, CancellationToken ct);

	public Task<bool> DeleteExtensionTokenAsync(CancellationToken ct);
	public bool ValidateToken(string token);
}

public class JwtService(
	TokenValidationParameters tokenValidationParameters,
	AppDbContext appDbContext
) : IJwtService
{
	private readonly TokenValidationParameters _tokenValidationParameters = tokenValidationParameters;
	private readonly AppDbContext _context = appDbContext;

	public async Task<TokenResponse> GetTokensAsync(ICollection<Claim> claims, UserProfile user, CancellationToken ct = default)
	{
		return new()
		{
			Token = GetAccessToken(claims),
			RefreshToken = await GetExtensionTokenAsync(user, ct)
		};
	}

	public string GetAccessToken(ICollection<Claim> claims, int expiresInMinutes = 3)
	{
		var tokenDescriptor = new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity(claims),
			Issuer = _tokenValidationParameters.ValidIssuer,
			Audience = _tokenValidationParameters.ValidAudience,
			Expires = DateTime.UtcNow.AddMinutes(expiresInMinutes),
			SigningCredentials = new(_tokenValidationParameters.IssuerSigningKey, SecurityAlgorithms.HmacSha256)
		};

		var handler = new JwtSecurityTokenHandler();
		return handler.CreateEncodedJwt(tokenDescriptor);
	}

	public async Task<string> GetExtensionTokenAsync(UserProfile user, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public bool ValidateToken(string token)
	{
		throw new NotImplementedException();
	}

	public async Task<bool> DeleteExtensionTokenAsync(CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}
}
