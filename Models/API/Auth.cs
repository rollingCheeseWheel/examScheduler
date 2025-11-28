using DataValidation;
using System.ComponentModel.DataAnnotations;

namespace Models.API;

public class OAuthRequest
{
	[Required]
	public required string AuthCode { get; set; }
	[Required]
	public required string SchoolId { get; set; }
}

public class TokenExtendRequest
{
	[Required]
	public required string RefreshToken { get; set; }
}

public class TokenResponse
{
	[Required]
	public required string AccessToken { get; set; }
	[Required]
	public required string RefreshToken { get; set; }
}