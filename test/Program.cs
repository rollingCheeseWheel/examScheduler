using Microsoft.Extensions.Configuration;
using registerClient;
using Util;

var config = new ConfigurationBuilder()
	.AddUserSecrets<Program>().Build();

Console.WriteLine($"{config[ "API:schoolUrl" ]}/");

Console.WriteLine("Authcode:");
var authCode = Console.ReadLine();

using var client = new RegisterClient(new(config["API:schoolUrl"]!), config["API:schoolId"]!, config["API:secret"]!, authCode!);

Console.WriteLine("UserProfile");
Console.WriteLine(await client.GetUserProfileAsync().ToJsonAsync(Constants.SerializerOptions));
Console.WriteLine("Classes");
Console.WriteLine(await client.GetClassesAsync().ToJsonAsync(Constants.SerializerOptions));
Console.WriteLine("Subjects");
Console.WriteLine(await client.GetSubjectsAsync().ToJsonAsync(Constants.SerializerOptions));
Console.WriteLine("Calendar week");
Console.WriteLine(await client.GetCalendarWeekAsync(DateTimeOffset.UtcNow) + "\teol");
Console.WriteLine("Upcoming Calendar");
Console.WriteLine(await client.GetUpcomingCalendarAsync() + "\teol");