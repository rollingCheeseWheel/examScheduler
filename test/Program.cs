using Microsoft.Extensions.Configuration;
using registerClient;
using Util;

var config = new ConfigurationBuilder()
	.AddUserSecrets<Program>().Build();

Console.WriteLine($"{config[ "API:schoolUrl" ]}v2/login/?client_id={config[ "API:clientId" ]}");

Console.Write("Authcode: ");
var authCode = Console.ReadLine();

using var client = new RegisterClient(new(config[ "API:schoolUrl" ]!), config[ "API:clientId" ]!, config[ "API:secret" ]!, authCode!);

//await client.AuthenticateAsync();
//Console.WriteLine("UserProfile");
//Console.WriteLine(await client.GetUserProfileAsync().ToJsonAsync());
//Console.WriteLine("Role");
//Console.WriteLine(await client.GetRoleAsync());
//Console.WriteLine("Classes");
//Console.WriteLine(await client.GetClassesAsync().ToJsonAsync());
//Console.WriteLine("Subjects");
//Console.WriteLine(await client.GetSubjectsAsync().ToJsonAsync());
//Console.WriteLine("Calendar week");
//Console.WriteLine(await client.GetCalendarWeekAsync(DateTimeOffset.UtcNow).ToJsonAsync());
//Console.WriteLine("Upcoming Calendar");
//Console.WriteLine(await client.GetUpcomingCalendarAsync().ToJsonAsync());

var calendar = await client.GetCompleteCalendarAsync(DateTimeOffset.UtcNow.AddDays(-30));
//calendar.Add(new() { Date = DateTimeOffset.UtcNow, Lessons = [ ] });
Console.WriteLine(calendar.ToJson());

//Console.WriteLine(await client.GetCalendarWeekAsync(DateTimeOffset.UtcNow.AddMonths(2)).ToJsonAsync());