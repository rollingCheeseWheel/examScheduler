using System.ComponentModel.DataAnnotations;

namespace Models.API;

public class OAuthRequest
{
	[Required]
	public required string AuthCode { get; set; }
	[Required]
	public required string SchoolId { get; set; }
}

public class AuthResponse
{
	public required DateTimeOffset Expiration { get; set; }
	public required UserProfile User { get; set; }
}