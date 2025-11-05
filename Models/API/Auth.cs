using DataValidation;
using System.ComponentModel.DataAnnotations;

namespace Models.API;

public class SignupRequest : AuthRequest
{
	[Required]
	public required string NewPassword { get; set; }
}

public class AuthRequest
{
	[Required]
	public required string Username { get; set; }
	[Required]
	public required string Password { get; set; }
	[Required, Url, UriValidator, MaxLength(300)]
	public required Uri RegisterUri { get; set; }
}

public class ExtendTokenRequest
{
	[Required]
	public required string RefreshToken { get; set; }
}

public class TokenResponse
{
	[Required]
	public required string Token { get; set; }
	[Required]
	public required string RefreshToken { get; set; }
}