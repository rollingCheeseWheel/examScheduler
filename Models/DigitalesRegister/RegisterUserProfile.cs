using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Util;
using Util.Converters;

namespace Models.DigitalesRegister;

public class RegisterUserProfile
{
	[Required]
	public required int Id { get; set; }
	[Required]
	public required string FirstName { get; set; }
	[Required]
	public required string LastName { get; set; }
	[Required, JsonConverter(typeof(StringEnumConverter<RegisterUserProfileRole>))]
	public required RegisterUserProfileRole Role { get; set; }

	public string? Picture { get; set; }
}

public class RegisterUserProfileRole : StringEnum
{
	public readonly RegisterUserProfileRole Student = new("student");
	public readonly RegisterUserProfileRole Teacher = new("teacher");
	public readonly RegisterUserProfileRole Parent = new("parent");
	public readonly RegisterUserProfileRole Admin = new("admin");

	protected RegisterUserProfileRole(string value) : base(value) { }
}