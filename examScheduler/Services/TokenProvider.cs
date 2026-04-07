using Entities;
using examScheduler.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Models.API;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Transactions;
using Util.Extensions;

namespace examScheduler.Services;

public interface ITokenProvider
{
	Task<TokenPair?> CreateTokenPairAsync(ICollection<Claim> claims, Entities.UserProfile user, CancellationToken ct);
	Task<TokenPair?> RefreshTokenPairAsync(ICollection<Claim> claims, string refreshToken, Entities.UserProfile user, CancellationToken ct);

	Task<bool> IsValidAccessTokenAsync(string accessToken, CancellationToken ct = default);
	Task<bool> IsValidRefreshTokenAsync(string refreshToken, Guid? userId = null, CancellationToken ct = default);

	Task RemoveStaleSessionsAsync(CancellationToken ct = default);
}

public class TokenProvider(
	JwtOptions options,
	AppDbContext context,
	ILogger<TokenProvider> logger
) : ITokenProvider
{
	private readonly JwtOptions _options = options;
	private readonly AppDbContext _context = context;
	private readonly ILogger _logger = logger;

	public async Task<bool> IsValidAccessTokenAsync(string accessToken, CancellationToken ct = default)
	{
		var jwtHandler = new JsonWebTokenHandler();
		var res = await jwtHandler.ValidateTokenAsync(accessToken, _options);
		return res.IsValid;
	}

	public async Task<bool> IsValidRefreshTokenAsync(string refreshToken, Guid? userId = null, CancellationToken ct = default)
	{
		if (userId.HasValue)
		{
			return await _context.RefreshSessions.AnyAsync(s =>
				s.TokenValue == refreshToken &&
				s.ExpirationDate >= DateTimeOffset.UtcNow &&
				s.UserProfileId == userId.Value
			, ct);
		}
		else
		{
			return await _context.RefreshSessions.AnyAsync(s =>
				s.TokenValue == refreshToken &&
				s.ExpirationDate >= DateTimeOffset.UtcNow
			, ct);
		}
	}

	public async Task<TokenPair?> CreateTokenPairAsync(ICollection<Claim> claims, Entities.UserProfile user, CancellationToken ct = default)
	{
		await RemoveStaleSessionsAsync(ct);
		var refreshToken = await CreateRefreshTokenAsync(user, ct);
		if (refreshToken is null)
		{
			_logger.LogInformation("failed to generate refresh token");
			return null;
		}
		var accessToken = GetAccessToken(claims);
		if (accessToken is null)
		{
			_logger.LogInformation("failed to generate access token");
			return null;
		}
		else
		{
			_logger.LogInformation("successfully generated access and refresh token");
			await _context.SaveChangesAsync(ct);
			return new(accessToken, refreshToken.TokenValue);
		}
	}

	public async Task<TokenPair?> RefreshTokenPairAsync(ICollection<Claim> claims, string refreshToken, Entities.UserProfile user, CancellationToken ct = default)
	{
		var existingValidSession = await _context.RefreshSessions.FirstOrDefaultAsync(s =>
			s.TokenValue == refreshToken &&
			s.UserProfileId == user.Id &&
			s.ExpirationDate == DateTimeOffset.UtcNow
		, ct);
		if (existingValidSession is null)
		{
			_logger.LogInformation("user does not have a refresh session");
			return null;
		}
		await DeleteRefreshTokenAsync(refreshToken, ct);
		var tokenPair = await CreateTokenPairAsync(claims, user, ct);
		if (tokenPair is not null)
		{
			_logger.LogInformation("successfully refreshed token");
		}
		return tokenPair;
	}

	private string? GetAccessToken(ICollection<Claim> claims)
	{
		var tokenDescriptor = new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity(claims),
			Issuer = _options.ValidIssuer,
			Audience = _options.ValidAudience,
			Expires = DateTime.UtcNow.AddMinutes(_options.TokenExpirationInMinutes),
			SigningCredentials = new(_options.IssuerSigningKey, SecurityAlgorithms.HmacSha256)
		};
		var handler = new JsonWebTokenHandler();
		return handler.CreateToken(tokenDescriptor);
	}

	private async Task<RefreshTokenSession?> CreateRefreshTokenAsync(Entities.UserProfile user, CancellationToken ct = default)
	{
		if (await _context.RefreshSessions.Where(s => s.UserProfileId == user.Id).CountAsync(ct) >= _options.MaxTokensPerUser)
		{
			return null;
		}
		else
		{
			var refreshToken = new RefreshTokenSession
			{
				ExpirationDate = DateTimeOffset.UtcNow.AddMinutes(_options.RefreshTokenExpirationInMinutes),
				UserProfileId = user.Id,
				TokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(_options.RefreshTokenBitStrength / 8))
			};
			await _context.RefreshSessions.AddAsync(refreshToken, ct);
			return refreshToken;
		}
	}

	private async Task DeleteRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
	{
		await _context.RefreshSessions
			.Where(s => s.TokenValue == refreshToken)
			.ExecuteDeleteAsync(ct);
	}

	public async Task RemoveStaleSessionsAsync(CancellationToken ct = default)
	{
		var removedSessionsCount = await _context.RefreshSessions
			.Where(s => s.ExpirationDate <= DateTimeOffset.UtcNow)
			.ExecuteDeleteAsync(ct);
		_logger.LogInformation("Removed {Count} stale sessions", removedSessionsCount);
	}
}

public class JwtOptions : TokenValidationParameters
{
	public required int RefreshTokenBitStrength { get; set; }
	public required double TokenExpirationInMinutes { get; set; }
	public required double RefreshTokenExpirationInMinutes { get; set; }
	public required int MaxTokensPerUser { get; set; }
}