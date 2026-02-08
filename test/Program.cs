using Microsoft.Extensions.Configuration;
using registerClient;
using System.Diagnostics;
using Util.Extensions;

var config = new ConfigurationBuilder()
	.AddUserSecrets<Program>().Build();
var sw = new Stopwatch();

Console.WriteLine($"{config[ "API:schoolUrl" ]}v2/login/?client_id={config[ "API:clientId" ]}");

Console.Write("Authcode: ");
var authCode = Console.ReadLine();

using var client = new DigitalRegisterClient(
	new(config[ "API:schoolUrl" ]!),
	config[ "API:clientId" ]!,
	config[ "API:secret" ]!,
	authCode!,
	400
);

sw.Start();
//await client.AuthenticateAsync();
//Console.WriteLine("UserProfile");
Console.WriteLine(await client.GetUserProfileAsync().StringifyAsync());
//Console.WriteLine("Role");
//Console.WriteLine(await client.GetRoleAsync());
Console.WriteLine("Classes");
Console.WriteLine(await client.GetClassesAsync().StringifyAsync());
Console.WriteLine("Subjects");
Console.WriteLine(await client.GetSubjectsAsync().StringifyAsync());
//Console.WriteLine("Calendar week");
//Console.WriteLine(await client.GetCalendarWeekAsync(DateTimeOffset.UtcNow).StringifyAsync());
//Console.WriteLine("Upcoming Calendar");
//Console.WriteLine(await client.GetUpcomingCalendarAsync().StringifyAsync());

//Console.WriteLine(await client.GetCalendarWeekAsync(DateTimeOffset.UtcNow).StringifyAsync());
//Console.WriteLine(await client.GetCalendarAsync(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7)).StringifyAsync());

//Console.WriteLine(await client.GetCalendarWeekAsync(DateTimeOffset.UtcNow).StringifyAsync());

//Console.WriteLine(await client.GetCalendarAsync(
//	DateTimeOffset.UtcNow.AddMonths(-4),
//	DateTimeOffset.UtcNow.AddMonths(1)
//).StringifyAsync());

//Console.WriteLine(await client.GetCalendarWeekAsync(DateTimeOffset.UtcNow.AddMonths(2)).StringifyAsync());

sw.Stop();
Console.WriteLine();
Console.WriteLine("Elapsed\t" + sw.Elapsed);