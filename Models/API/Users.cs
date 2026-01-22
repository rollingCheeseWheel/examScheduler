using System.ComponentModel.DataAnnotations;
using Util;

namespace Models.API;

public class UserProfile
{
	[Required]
	public required Guid Id { get; set; }
	[Required]
	public required string Name { get; set; }
	[Required]
	public required UserRole Role { get; init; }
}