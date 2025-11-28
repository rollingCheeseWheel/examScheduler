using Entities;
using examScheduler.Data;
using Isopoh.Cryptography.SecureArray;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Models.API;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Transactions;
using Util;

namespace examScheduler.Services;

public interface ITokenProvider
{
	Task<TokenResponse?> GetTokenPairAsync(ICollection<Claim> claims, UserProfile user, CancellationToken ct);
	Task<TokenValidationResult?> TryValidateTokenAsync(UserProfile user, string token, CancellationToken ct);

	//string? GetAccessToken(ICollection<Claim> claims);
	//Task<RefreshTokenSession?> CreateRefreshTokenAsync(UserProfile user, CancellationToken ct);

	Task<TokenResponse?> RefreshTokenPairAsync(ICollection<Claim> claims, string refreshToken, UserProfile user, CancellationToken ct);

	Task DeleteRefreshTokenAsync(string refreshToken, CancellationToken ct);
	Task DeleteAllRefreshTokensForUserAsync(UserProfile user);
}

public class TokenProvider(
	JwtOptions options,
	AppDbContext context
) : ITokenProvider
{
	private readonly JwtOptions _options = options;
	private readonly AppDbContext _context = context;

	public async Task<TokenValidationResult?> TryValidateTokenAsync(UserProfile user, string token, CancellationToken ct = default) => await new JsonWebTokenHandler().ValidateTokenAsync(token, _options).WaitAsync(ct);

	public async Task<TokenResponse?> GetTokenPairAsync(ICollection<Claim> claims, UserProfile user, CancellationToken ct = default)
	{
		var refreshToken = await CreateRefreshTokenAsync(user, ct);
		if (refreshToken is null) { return null; }
		var accessToken = GetAccessToken(claims);
		if (accessToken is null) { return null; }
		return new()
		{
			RefreshToken = refreshToken.TokenValue,
			AccessToken = accessToken,
		};
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

	public async Task<TokenResponse?> RefreshTokenPairAsync(ICollection<Claim> claims, string refreshToken, UserProfile user, CancellationToken ct = default)
	{
		using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
		var existingValidSession = await HasValidRefreshTokenSessionAsync(user, refreshToken, ct);
		if (existingValidSession is null) { return null; }
		await DeleteRefreshTokenAsync(refreshToken, ct);
		return await GetTokenPairAsync(claims, user, ct);
	}

	public async Task<RefreshTokenSession?> CreateRefreshTokenAsync(UserProfile user, CancellationToken ct = default)
	{
		if (await GetSessionsForUserIQueryable(user).CountAsync(ct) >= _options.MaxTokensPerUser)
		{
			return null;
		}
		else
		{
			var refreshToken = new RefreshTokenSession
			{
				Id = Guid.NewGuid(),
				ExpirationDate = DateTimeOffset.UtcNow.AddMinutes(_options.RefreshTokenExpirationInMinutes),
				UserProfileId = user.Id,
				TokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(_options.RefreshTokenBitStrength / 8))
			};
			_context.RefreshSessions.Add(refreshToken);
			return refreshToken;
		}
	}

	public async Task DeleteAllRefreshTokensForUserAsync(UserProfile user)
	{
		var tokens = await GetSessionsForUserAsync(user);
		if (tokens is null) { return; }
		_context.RemoveRange(tokens);
	}

	public async Task DeleteRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
	{
		var entity = await _context.RefreshSessions.FirstOrDefaultAsync(t => t.TokenValue == refreshToken, ct);
		if (entity is null) { return; }
		_context.RefreshSessions.Remove(entity);
	}
	private async Task<RefreshTokenSession?> HasValidRefreshTokenSessionAsync(UserProfile user, string refreshTokenValue, CancellationToken ct = default) => await GetSessionsForUserIQueryable(user).FirstOrDefaultAsync(t => t.TokenValue == refreshTokenValue && t.ExpirationDate > DateTimeOffset.UtcNow, ct);

	private async Task<ICollection<RefreshTokenSession>> GetSessionsForUserAsync(UserProfile user, CancellationToken ct = default) => await GetSessionsForUserIQueryable(user).ToListAsync(ct);

	private IQueryable<RefreshTokenSession> GetSessionsForUserIQueryable(UserProfile user) => _context.RefreshSessions.Where(s => s.UserProfileId == user.Id);
}

public class JwtOptions : TokenValidationParameters
{
	public required int RefreshTokenBitStrength { get; set; }
	public required double TokenExpirationInMinutes { get; set; }
	public required double RefreshTokenExpirationInMinutes { get; set; }
	public required int MaxTokensPerUser { get; set; }
}