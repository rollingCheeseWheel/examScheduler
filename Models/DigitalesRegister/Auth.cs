using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Models.DigitalesRegister;

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