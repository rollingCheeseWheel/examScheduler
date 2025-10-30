namespace Models.DigitalesRegister;

public class RegisterProfileModel
{
	public required string Username { get; set; }
	public required string RoleName { get; set; }
	/// <summary>
	/// could be parsed with this regex <c>^(\w+).*?(\w)?\w*$</c> to extract the first first name and the first letter of the last name
	/// </summary>
	public required string Name { get; set; } 
	public required string Email { get; set; }
	public required string Picture { get; set; }
	public string? Language { get; set; }
}