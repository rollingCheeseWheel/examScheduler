using registerClient;
using Microsoft.Extensions.Configuration;
using Util;
using System.Text.Json;
using Models.DigitalesRegister;

var options = new JsonSerializerOptions { WriteIndented = true };

var config = new ConfigurationBuilder()
	.AddUserSecrets<Program>().Build();

using var client = new RegisterClient(config[ "username" ]!, config[ "password" ]!, "https://wfo-bruneck.digitalesregister.it/");

//Console.WriteLine("profile details: " + (await client.GetUserProfileAsync()).ToJson(options));

//Console.WriteLine("current calendar week:\n" + await client.GetCurrentCalendarWeekString());

var week = await client.GetCalendarWeekAsync(DateTime.Now.AddDays(14));

var calendar = new Calendar { Data = [ week ] };

Console.WriteLine(calendar.CompileTeachersWithSubject().ToJson(options));


/*var password = "bobber";
var hash = PasswordHelper.Hash(password);
Console.WriteLine(hash);
Console.WriteLine(PasswordHelper.Verify(hash, password));
Console.WriteLine(PasswordHelper.Verify(hash, "hash"));*/





/*using var handler = new HttpClientHandler()
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
var responseMessage = await response.Content.ReadAsStringAsync();
Console.WriteLine(responseMessage);


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
}*/
