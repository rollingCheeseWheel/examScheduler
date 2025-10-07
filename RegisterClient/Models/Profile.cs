namespace registerClient.Models;

public class Profile
{
	public required string Username { get; set; }
	public required string RoleName { get; set; }
	public required string Name { get; set; }
	public required string Email { get; set; }
	public required Twofactor TwoFactor { get; set; }
	public required string Picture { get; set; }
	public required bool NotificationsEnabled { get; set; }
	public required bool NotificationsSubstitutionsEnabled { get; set; }
	public required string Language { get; set; }
}

public class Twofactor
{
	public required int Enabled { get; set; }
	public required string Two_factor { get; set; }
	public required string Qr_code { get; set; }
}
