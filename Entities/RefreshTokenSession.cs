using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities;

public class RefreshTokenSession
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; private set; }
	[Required]
	public required string RefreshToken { get; set; }
	[Required]
	public required UserProfile UserProfile { get; set; }
}
