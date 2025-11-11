using registerClient;
using Microsoft.Extensions.Configuration;
using Util;
using System.Text.Json;
using Models.DigitalesRegister;
using System.Diagnostics;

var options = new JsonSerializerOptions { WriteIndented = true };

/*Console.WriteLine("Benutzername");
var username = Console.ReadLine();
Console.WriteLine("Passwort");
var password = Console.ReadLine();
Console.Clear();*/

var sw = new Stopwatch();

var config = new ConfigurationBuilder()
	.AddUserSecrets<Program>().Build();

using var client = new RegisterClientWeb(config[ "username" ]!, config[ "password" ]!, "https://wfo-bruneck.digitalesregister.it/");
//using var client = new RegisterClient(username, password, "https://wfo-bruneck.digitalesregister.it/");

//Console.WriteLine("profile details: " + (await client.GetUserProfileAsync()).ToJson(options));

//Console.WriteLine("current calendar week:\n" + await client.GetCurrentCalendarWeekString());

//sw.Start();
Console.WriteLine(await client.GetUserProfileAsync().ToJsonAsync(options));
//sw.Print().Stop();

for (var i = 0; i < 10; i++)
	Console.WriteLine();


//sw.Restart();
/*var calendar = await client.GetCompleteCalendarAsync();*/
//sw.Print().Restart();
/*Console.WriteLine(calendar?.CompileTeachersWithSubjects().ToJson(options));*/
//sw.Print().Stop();