using System.ComponentModel.DataAnnotations;
using Util;
using Util.Validation;

namespace Models.API;

public class UserProfile
{
	[Required]
	public required Guid Id { get; set; }
	[Required]
	public required string Name { get; set; }
	[Required, ValidEnum]
	public required UserRoles Role { get; set; }
}