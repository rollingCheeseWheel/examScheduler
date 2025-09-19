namespace examScheduler.Digitales_Register_API.Models;

public class LoginRequest
{
	public required string Username { get; set; }
	public required string Password { get; set; }
}

public class LoginResponse
{
	public required object? Error { get; set; }
	public required bool LoggedIn { get; set; }
}