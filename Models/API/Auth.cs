using System.ComponentModel.DataAnnotations;

namespace Models.API;

public class OAuthRequest
{
	[Required]
	public required string AuthCode { get; set; }
	[Required]
	public required string SchoolId { get; set; }
}