using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities;

public class RefreshTokenSession
{
	[Key]
	public required Guid Id { get; set; }
	[Required]
	public required DateTimeOffset ExpirationDate { get; set; }
	[Required]
	public required string TokenValue { get; set; }
	[Required]
	public required UserProfile UserProfile { get; set; }
}
