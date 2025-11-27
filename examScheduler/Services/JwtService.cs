using Entities;
using Microsoft.IdentityModel.Tokens;
using Models.API;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Util;

namespace examScheduler.Services;

public interface IJwtService
{
	TokenResponse? GetTokenPairs(ICollection<Claim> claims, UserProfile user);
	string? GetAccessToken(ICollection<Claim> claims, Guid refreshTokenId);
	RefreshTokenSession? GetRefreshToken(UserProfile user);

	TokenResponse? RefreshTokens(ICollection<Claim> claims, string refreshToken, UserProfile user);

	bool TryDeleteRefreshToken(UserProfile user, string refreshToken);
	void DeleteAllRefreshTokens(UserProfile user);
	ClaimsPrincipal? TryValidateToken(UserProfile user, string token);
}

public class JwtService(JwtOptions jwtOptions) : IJwtService
{
	private readonly JwtOptions _jwtOptions = jwtOptions;
	public const string JWTRefreshTokenClaimName = "RefreshToken";

	public TokenResponse? GetTokenPairs(ICollection<Claim> claims, UserProfile user)
	{
		var refreshToken = GetRefreshToken(user);
		if (refreshToken is null) { return null; }
		var accessToken = GetAccessToken(claims, refreshToken.Id);
		if (accessToken is null) { return null; }
		return new()
		{
			Token = accessToken,
			RefreshToken = refreshToken.TokenValue
		};
	}

	public string? GetAccessToken(ICollection<Claim> claims, Guid refreshTokenId)
	{
		claims.Add(new Claim(JWTRefreshTokenClaimName, refreshTokenId.ToString()));

		var tokenDescriptor = new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity(claims),
			Issuer = _jwtOptions.ValidIssuer,
			Audience = _jwtOptions.ValidAudience,
			Expires = DateTime.UtcNow.AddMinutes(_jwtOptions.TokenExpirationInMinutes),
			SigningCredentials = new(_jwtOptions.IssuerSigningKey, SecurityAlgorithms.HmacSha256)
		};

		var handler = new JwtSecurityTokenHandler();
		return handler.CreateEncodedJwt(tokenDescriptor);
	}

	public RefreshTokenSession? GetRefreshToken(UserProfile user)
	{
		if (user.RefreshTokens.Count >= _jwtOptions.MaxTokensPerUser)
		{
			return null;
		}
		else
		{
			var refreshToken = new RefreshTokenSession
			{
				Id = Guid.NewGuid(),
				ExpirationDate = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.RefreshTokenExpirationInMinutes),
				UserProfile = user,
				TokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(_jwtOptions.RefreshTokenBitStrength / 8))
			};
			user.RefreshTokens.Add(refreshToken);
			return refreshToken;
		}
	}

	public TokenResponse? RefreshTokens(ICollection<Claim> claims, string refreshToken, UserProfile user)
	{
		var existingRefreshSession = GetValidRefreshTokenSession(user, refreshToken);
		if (existingRefreshSession is null) { return null; }
		if (!TryDeleteRefreshToken(user, refreshToken)) { return null; }
		return GetTokenPairs(claims, user);
	}

	public bool TryDeleteRefreshToken(UserProfile user, string refreshToken)
	{
		var token = user.RefreshTokens.FirstOrDefault(t => t.TokenValue == refreshToken);
		if (token is null) { return false; }
		return user.RefreshTokens.Remove(token);
	}

	public void DeleteAllRefreshTokens(UserProfile user) => user.RefreshTokens.Clear();

	public ClaimsPrincipal? TryValidateToken(UserProfile user, string accessToken)
	{
		try
		{
			var claimsPrincipal = new JwtSecurityTokenHandler().ValidateToken(accessToken, _jwtOptions, out var _);
			var refreshTokenClaim = claimsPrincipal.FindFirst(JWTRefreshTokenClaimName);
			if (refreshTokenClaim is null) { return null; }
			if (!user.RefreshTokens.Any(t => t.TokenValue == refreshTokenClaim.Value))
			{ // invalid refresh accessToken
				return null;
			}
			return claimsPrincipal;
		}
		catch
		{
			return null;
		}
	}

	private RefreshTokenSession? GetValidRefreshTokenSession(UserProfile user, string refreshTokenValue) => user.RefreshTokens.FirstOrDefault(t => t.TokenValue == refreshTokenValue && t.ExpirationDate > DateTimeOffset.UtcNow);
}


public class JwtOptions : TokenValidationParameters
{
	public required int RefreshTokenBitStrength { get; set; }
	public required double TokenExpirationInMinutes { get; set; }
	public required double RefreshTokenExpirationInMinutes { get; set; }
	public required int MaxTokensPerUser { get; set; }
}