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
	TokenResponse? GetTokens(ICollection<Claim> claims, UserProfile user);
	string? GetAccessToken(ICollection<Claim> claims, RefreshTokenSession RefreshToken);
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

	public TokenResponse? GetTokens(ICollection<Claim> claims, UserProfile user)
	{
		var refreshToken = GetRefreshToken(user);
		if (refreshToken is null) { return null; }
		var accessToken = GetAccessToken(claims, refreshToken);
		if (accessToken is null) { return null; }
		return new()
		{
			Token = accessToken,
			RefreshToken = refreshToken.RandomString
		};
	}

	public string? GetAccessToken(ICollection<Claim> claims, RefreshTokenSession refreshToken)
	{
		claims.Add(new Claim(JWTRefreshTokenClaimName, refreshToken.RandomString));

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
			var randomStringBase = Convert.ToBase64String(RandomNumberGenerator.GetBytes((int)( _jwtOptions.RefreshTokenBitStrength / 8 )));
			var refreshToken = new RefreshTokenSession
			{
				ExpirationDate = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.TokenExpirationInMinutes + _jwtOptions.RefreshTokenExpirationOffset),
				UserProfile = user,
				RandomString = randomStringBase
			};

			user.RefreshTokens.Add(refreshToken);
			return refreshToken;
		}
	}

	public TokenResponse? RefreshTokens(ICollection<Claim> claims, string refreshToken, UserProfile user)
	{
		throw new NotImplementedException();
	}

	public bool TryDeleteRefreshToken(UserProfile user, string refreshToken)
	{
		var token = user.RefreshTokens.FirstOrDefault(t => t.RandomString == refreshToken);
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
			if (!user.RefreshTokens.Any(t => t.RandomString == refreshTokenClaim.Value))
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
}


public class JwtOptions : TokenValidationParameters
{
	public required int RefreshTokenBitStrength { get; set; }
	public required double TokenExpirationInMinutes { get; set; }
	public required double RefreshTokenExpirationOffset { get; set; }
	public required int MaxTokensPerUser { get; set; }
}