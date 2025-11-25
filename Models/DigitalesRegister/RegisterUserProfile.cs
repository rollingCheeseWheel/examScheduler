using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Util;
using Util.Converters;

namespace Models.DigitalesRegister;

public class RegisterUserProfile
{
	/// <summary>
	/// Maps to UserProfile.Username
	/// </summary>
	[Required]
	public required int Id { get; set; }
	public string? FirstName { get; set; }
	public string? LastName { get; set; }
	public string? Role { get; set; }

	public string? Picture { get; set; }
	public StudentData? StudentData { get; set; }
}

public class StudentData
{
	public int? Id { get; set; }
	[JsonPropertyName("name")]
	public string? JoinedName { get; set; }
	[JsonPropertyName("firstName")]
	public string? Name { get; set; }
	[JsonPropertyName("lastName")]
	public string? Surname { get; set; }
	[JsonPropertyName("mainclass")]
	public required StudenProfileClass? MainClass { get; set; }
	[JsonPropertyName("classes")]
	public required StudenProfileClass[ ] OtherClasses { get; set; } = [ ];
}

public class StudenProfileClass
{
	public int? Id { get; set; }
	[Required]
	public required string Name { get; set; }
	[JsonConverter(typeof(IntToBoolConverter))]
	public bool ChoiceSubject { get; set; } = false;
	[JsonConverter(typeof(IntToBoolConverter))]
	public bool belongsTo { get; set; } = false;
}

public class RegisterUserProfileRole : StringEnum
{
	public readonly RegisterUserProfileRole Student = new("student");
	public readonly RegisterUserProfileRole Teacher = new("teacher");
	public readonly RegisterUserProfileRole Parent = new("parent");
	public readonly RegisterUserProfileRole Admin = new("admin");

	protected RegisterUserProfileRole(string value) : base(value) { }
}