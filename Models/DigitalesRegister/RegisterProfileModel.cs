using System.ComponentModel.DataAnnotations;

namespace Models.DigitalesRegister;

public class RegisterProfileModel
{
	[Required]
	public required string Username { get; set; }
	[Required]
	public required string RoleName { get; set; }
	/// <summary>
	/// could be parsed with this regex <c>^(\w+).*?(\w)?\w*$</c> to extract the first first name and the first letter of the last name
	/// </summary>
	[Required]
	public required string Name { get; set; }
	[Required, EmailAddress]
	public required string Email { get; set; }
	[Required]
	public required string Picture { get; set; }
	public string? Language { get; set; }
}