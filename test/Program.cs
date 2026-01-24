using Microsoft.Extensions.Configuration;
using registerClient;
using System.Diagnostics;
using Util;

var config = new ConfigurationBuilder()
	.AddUserSecrets<Program>().Build();
var sw = new Stopwatch();

Console.WriteLine($"{config[ "API:schoolUrl" ]}v2/login/?client_id={config[ "API:clientId" ]}");

Console.Write("Authcode: ");
var authCode = Console.ReadLine();

using var client = new RegisterClient(
	new(config[ "API:schoolUrl" ]!),
	config[ "API:clientId" ]!,
	config[ "API:secret" ]!,
	authCode!,
	400
);

sw.Start();
//await client.AuthenticateAsync();
//Console.WriteLine("UserProfile");
Console.WriteLine(await client.GetUserProfileAsync().ToJsonAsync());
//Console.WriteLine("Role");
//Console.WriteLine(await client.GetRoleAsync());
Console.WriteLine("Classes");
Console.WriteLine(await client.GetClassesAsync().ToJsonAsync());
Console.WriteLine("Subjects");
Console.WriteLine(await client.GetSubjectsAsync().ToJsonAsync());
//Console.WriteLine("Calendar week");
//Console.WriteLine(await client.GetCalendarWeekAsync(DateTimeOffset.UtcNow).ToJsonAsync());
//Console.WriteLine("Upcoming Calendar");
//Console.WriteLine(await client.GetUpcomingCalendarAsync().ToJsonAsync());

//Console.WriteLine(await client.GetCalendarWeekAsync(DateTimeOffset.UtcNow).ToJsonAsync());
//Console.WriteLine(await client.GetCalendarAsync(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7)).ToJsonAsync());

//Console.WriteLine(await client.GetCalendarWeekAsync(DateTimeOffset.UtcNow).ToJsonAsync());

//Console.WriteLine(await client.GetCalendarAsync(
//	DateTimeOffset.UtcNow.AddMonths(-4),
//	DateTimeOffset.UtcNow.AddMonths(1)
//).ToJsonAsync());

//Console.WriteLine(await client.GetCalendarWeekAsync(DateTimeOffset.UtcNow.AddMonths(2)).ToJsonAsync());

sw.Stop();
Console.WriteLine();
Console.WriteLine("Elapsed\t" + sw.Elapsed);