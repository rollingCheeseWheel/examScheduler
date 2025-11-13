using System.ComponentModel.DataAnnotations;

namespace Models.DigitalesRegister;

public class RegisterUserProfile
{
	[Required]
	public required int Id { get; set; }
	[Required]
	public required string FirstName { get; set; }
	[Required]
	public required string LastName { get; set; }
	[Required]
	public required string Role { get; set; }

	public string? Picture { get; set; }
}
