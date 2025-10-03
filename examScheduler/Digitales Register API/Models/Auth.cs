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
	[JsonPropertyName("loggedIn")]
	public bool? LoggedIn { get; set; }
}