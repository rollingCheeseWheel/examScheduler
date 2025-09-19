using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

var config = new ConfigurationBuilder()
	.AddUserSecrets<Program>().Build();

using var handler = new HttpClientHandler()
{
	CookieContainer = new(),
	UseCookies = true
};

using var client = new HttpClient(handler);

var uri = new Uri("https://wfo-bruneck.digitalesregister.it/v2/api/auth/login");
var cookieUri = new Uri(uri.GetLeftPart(UriPartial.Authority));

var credentials = new
{
	password = config[ "password" ],
	username = config[ "username" ]
};

var stringContent = new StringContent(JsonSerializer.Serialize(credentials))
{
	Headers = { ContentType = new("application/json") }
};

var request = new HttpRequestMessage(HttpMethod.Post, uri)
{
	Content = stringContent
};

var response = await client.SendAsync(request);

handler.CookieContainer.PrintAllCookies("POST");
Console.WriteLine(await response.Content.ReadAsStringAsync());


public static class Extensions
{
	private static int printCounter = 1;

	public static void PrintAllCookies(this CookieContainer cookieContainer, string prefix, Uri? cookieUri = null)
	{
		Console.WriteLine($"{printCounter++} {prefix}");
		if (cookieUri is not null)
		{
			foreach (Cookie cookie in cookieContainer.GetCookies(cookieUri))
			{
				Console.WriteLine($"{cookie.Domain}{cookie.Path} {cookie.Name} = {cookie.Value}");
			}
		}
		else
		{
			foreach (Cookie cookie in cookieContainer.GetAllCookies())
			{
				Console.WriteLine($"{cookie.Domain}{cookie.Path} {cookie.Name} = {cookie.Value}");
			}
		}
	}
}