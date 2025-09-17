namespace examScheduler.Models.Auth;

public class RegisterRequest
{
	public required string RegisterUsername { get; set; }
	public required string RegisterPassword { get; set; }
	public required Uri RegisterUri { get; set; }
}