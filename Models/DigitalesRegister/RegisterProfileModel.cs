namespace Models.DigitalesRegister;

public class RegisterProfileModel
{
	public required string Username { get; set; }
	public required string RoleName { get; set; }
	public required string Name { get; set; }
	public required string Email { get; set; }
	public required string Picture { get; set; }
	public string? Language { get; set; }
}