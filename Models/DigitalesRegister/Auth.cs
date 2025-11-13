using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Routing.Constraints;

namespace Models.DigitalesRegister;

[Obsolete]
public class LoginRequest
{
	[JsonPropertyName("username")]
	public required string Username { get; set; }
	[JsonPropertyName("password")]
	public required string Password { get; set; }
}

[Obsolete]
public class LoginResponse
{
	[JsonPropertyName("loggedIn")]
	public bool? LoggedIn { get; set; }
}

public class TokenCreateRequest
{
	[Required]
	public required string Code { get; set; }
}

public class TokenCreateResponse
{
	[Required]
	[JsonPropertyName("user_id")]
	public required int UserId { get; set; }
	[Required]
	[JsonPropertyName("expiration")]
	public required DateTimeOffset ExpirationDate { get; set; }
	[Required]
	public required string Token { get; set; }
	[Required]
	[JsonPropertyName("refresh_token")]
	public required string RefreshToken { get; set; }
}

public class TokenRefreshRequest
{
	[Required]
	[JsonPropertyName("user_id")]
	public required int UserId { get; set; }
	[Required]
	[JsonPropertyName("refresh_token")]
	public required string RefreshToken { get; set; }
}

public class TokenRefreshResponse
{
	[Required]
	public required string Token { get; set; }
	[Required]
	public required DateTimeOffset Expiration { get; set; }
}