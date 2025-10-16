namespace Models.Auth;

public class RegisterRequest
{
	public required Uri RegisterUri { get; set; }
	public required string Username { get; set; }
	public required string RegisterPassword { get; set; }
	public required string AccountPassword { get; set; }
}