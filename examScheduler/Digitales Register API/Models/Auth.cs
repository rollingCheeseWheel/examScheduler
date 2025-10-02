using System.Text.Json.Serialization;

namespace examScheduler.Digitales_Register_API.Models;

public class LoginRequest
{
	[JsonPropertyName("username")]
	public required string Username { get; set; }
	[JsonPropertyName("password")]
	public required string Password { get; set; }
}

public class LoginResponse
{
	[JsonPropertyName("error")]
	public required object? Error { get; set; }
	[JsonPropertyName("loggedIn")]
	public required bool LoggedIn { get; set; }
}