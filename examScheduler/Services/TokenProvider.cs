using Entities;
using examScheduler.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Models.API;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Transactions;
using Util.Extensions;

namespace examScheduler.Services;

public interface ITokenProvider
{
	Task<TokenPair?> GetTokenPairAsync(ICollection<Claim> claims, Entities.UserProfile user, CancellationToken ct);
	Task<TokenPair?> RefreshTokenPairAsync(ICollection<Claim> claims, string refreshToken, Entities.UserProfile user, CancellationToken ct);
	Task<TokenValidationResult?> TryValidateTokenAsync(Entities.UserProfile user, string token, CancellationToken ct);

	Task DeleteRefreshTokenAsync(string refreshToken, CancellationToken ct);
	Task DeleteAllRefreshTokensForUserAsync(Entities.UserProfile user, CancellationToken ct);
}

public class TokenProvider(
	JwtOptions options,
	AppDbContext context
) : ITokenProvider
{
	private readonly JwtOptions _options = options;
	private readonly AppDbContext _context = context;

	public async Task<TokenValidationResult?> TryValidateTokenAsync(Entities.UserProfile user, string token, CancellationToken ct = default) => await new JsonWebTokenHandler().ValidateTokenAsync(token, _options).WaitAsync(ct);

	public async Task<TokenPair?> GetTokenPairAsync(ICollection<Claim> claims, Entities.UserProfile user, CancellationToken ct = default)
	{
		await RemoveExpiredSessionsForUserAsync(ct);
		var refreshToken = await CreateRefreshTokenAsync(user, ct);
		if (refreshToken is null) { return null; }
		var accessToken = GetAccessToken(claims);
		return accessToken is null
			? null
			: new(refreshToken.TokenValue, accessToken);
	}

	public string? GetAccessToken(ICollection<Claim> claims)
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

	public async Task<TokenPair?> RefreshTokenPairAsync(ICollection<Claim> claims, string refreshToken, Entities.UserProfile user, CancellationToken ct = default)
	{
		await RemoveExpiredSessionsForUserAsync(ct);
		using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
		var existingValidSession = await _context.RefreshSessions.FirstOrDefaultAsync(s => s.TokenValue == refreshToken && s.UserProfileId == user.Id, ct);
		if (existingValidSession is null) { return null; }
		await DeleteRefreshTokenAsync(refreshToken, ct);
		return await GetTokenPairAsync(claims, user, ct);
	}

	public async Task<RefreshTokenSession?> CreateRefreshTokenAsync(Entities.UserProfile user, CancellationToken ct = default)
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
			_context.RefreshSessions.Add(refreshToken);
			return refreshToken;
		}
	}

	public async Task DeleteAllRefreshTokensForUserAsync(Entities.UserProfile user, CancellationToken ct = default)
	{
		await _context.RefreshSessions
			.Where(s => s.UserProfileId == user.Id)
			.ExecuteDeleteAsync(ct);
	}

	public async Task DeleteRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
	{
		await _context.RefreshSessions
			.Where(s => s.TokenValue == refreshToken)
			.ExecuteDeleteAsync(ct);
	}

	private async Task RemoveExpiredSessionsForUserAsync(CancellationToken ct = default)
	{
		await _context.RefreshSessions
			.Where(s => s.ExpirationDate <= DateTimeOffset.UtcNow)
			.ExecuteDeleteAsync(ct);
		await _context.SaveChangesAsync(ct);
	}
}

public class JwtOptions : TokenValidationParameters
{
	public required int RefreshTokenBitStrength { get; set; }
	public required double TokenExpirationInMinutes { get; set; }
	public required double RefreshTokenExpirationInMinutes { get; set; }
	public required int MaxTokensPerUser { get; set; }
}