using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Util;
using Util.Converters;
using Util.Validation;

namespace Models.API;

public class UserProfile
{
	[Required]
	public required Guid Id { get; set; }
	[Required]
	public required string Name { get; set; }
	[Required, DefinedEnum, JsonConverter(typeof(EnumConverter<UserRoles>))]
	public required UserRoles Role { get; set; }
}