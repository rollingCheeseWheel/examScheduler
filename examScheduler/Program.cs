using Entities;
using examScheduler.BackgroundServices;
using examScheduler.Data;
using examScheduler.Hubs;
using examScheduler.Misc;
using examScheduler.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;
using System.Text.Json;
using Util;
using Util.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddCors(options =>
{
	options.AddPolicy("CORS", p =>
		p.WithOrigins("https://examscheduler.app/", "https://localhost/")
		.AllowAnyHeader()
		.AllowAnyMethod()
		.AllowCredentials()
	);
});

var keyvaultConnectionString = builder.Configuration.GetConnectionString(ResourceNames.KeyVault);
if (keyvaultConnectionString is not null)
{
	builder.Configuration.AddAzureKeyVaultSecrets(keyvaultConnectionString);
}

// Add services
builder.Services.AddDbContext<AppDbContext>(options =>
{
	options.UseNpgsql(
		builder.Configuration.GetConnectionString(ResourceNames.DBName) + ";Include Error Detail=true",
		o =>
		{
			o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
		}
	);

	//options.EnableSensitiveDataLogging();
	//options.EnableDetailedErrors();
	//options.LogTo(Console.WriteLine, LogLevel.Information);
});

builder.Services
	.AddIdentity<UserProfile, IdentityRole<Guid>>()
	.AddEntityFrameworkStores<AppDbContext>()
	.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
	options.Cookie.HttpOnly = true;
	options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
	options.Cookie.SameSite = SameSiteMode.Lax;
	options.Cookie.Path = "/";

	options.ExpireTimeSpan = TimeSpan.FromHours(1);
	options.SlidingExpiration = true;

	options.Events.OnRedirectToLogin = ctx =>
	{
		ctx.Response.StatusCode = 401;
		return Task.CompletedTask;
	};

	options.Events.OnRedirectToAccessDenied = ctx =>
	{
		ctx.Response.StatusCode = 403;
		return Task.CompletedTask;
	};
});

builder.Services.AddAuthorization();

builder.Services.AddSignalR(options =>
	{
		options.EnableDetailedErrors = true;
	}
);

/*// services //*/
builder.Services
	.AddScoped<IAuthService, AuthService>()
	.AddScoped<ICalendarService, CalendarService>()
	.AddScoped<IClassroomService, ClassroomService>()
	.AddScoped<IScheduleService, ScheduleService>()
	.AddScoped<ISchoolsService, SchoolsService>();
/*////*/

/*// singletons //*/
builder.Services
	.AddSingleton<IDigitalRegisterClientService, DigitalRegisterClientService>();
/*////*/

builder.Services.AddTransient<HttpsEnforcingHandler>();
builder.Services.AddHttpClient("secure")
	.AddHttpMessageHandler<HttpsEnforcingHandler>();

/*// background workers //*/
builder.Services
	.AddHostedService<IEventWorker, EventWorker>();
/*////*/

builder.Services.AddControllers()
	.AddJsonOptions(options =>
	{
		// remember to also change the settings in Constants.SerializerOptions
		options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
		options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
		options.JsonSerializerOptions.AllowTrailingCommas = true;
	});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
/*builder.Services.AddOpenApi("openapi");*/

//builder.Services.AddResponseCompression(options =>
//{
//	options.EnableForHttps = true;
//	options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
//		[ /*"application/json",*/ "application/javascript", "style/css", "text/html" ]
//	);
//});

var app = builder.Build();

if (app.Environment.IsProduction())
{
	app.UseExceptionHandler(options =>
	{
		options.Run(async context =>
		{
			context.Response.StatusCode = 500;
			context.Response.ContentType = "application/json";

			await context.Response.WriteAsJsonAsync<Models.API.Result<object>>(
				new(System.Net.HttpStatusCode.InternalServerError,
					"Internal Server Error")
			);
		});
	});
}

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	/*app.MapOpenApi("{documentName}");*/

	using var schoolScope = app.Services.CreateScope();
	var db = schoolScope.ServiceProvider.GetRequiredService<AppDbContext>();
	db.Database.Migrate();

	List<School> schools = [
		new()
		{
			Name = "Test WFO Bruneck Innichen",
			RegisterUri = new("https://wfo-test-bruneck.digitalesregister.it/"),
			SchoolId = "wfo-test-bruneck",
			ClientId = "vHMQataCe5HKAzDr",
			Secret = app.Configuration["vHMQataCe5HKAzDr"]!,
			IsEnabled = true
		},
		new() {
			Name = "some school",
			RegisterUri = new("https://some-school.digitalesregister.it/"),
			SchoolId = "some-school",
			ClientId = "asdfölijasdlfkjhask",
			Secret = "alsdkhjfgxcvhyölhdfjlhasgu",
			IsEnabled = true,
		},
		new() {
			Name = "some other school",
			RegisterUri = new("https://some-other-school.digitalesregister.it/"),
			SchoolId = "some-other-school",
			ClientId = "kkjdhzfgszdgfjkahakjs",
			Secret = "alsdkhjfgxcvhyölhdfjlhasgu",
			IsEnabled = false,
		}
	];

	var existingSchools = await db.Schools.AsNoTracking().ToListAsync(app.Lifetime.ApplicationStopping);
	if (!schools.ValueEquals(existingSchools))
	{
		await db.Schools.ExecuteDeleteAsync(app.Lifetime.ApplicationStopping);
		await db.Schools.AddRangeAsync(schools, app.Lifetime.ApplicationStopped);
		await db.SaveChangesAsync(app.Lifetime.ApplicationStopping);
	}
}

app.UseStaticFiles(new StaticFileOptions
{
	OnPrepareResponse = ctx =>
	{
		var path = ctx.File.PhysicalPath;
		ctx.Context.Response.Headers.CacheControl = path is not null && path.EndsWith("html") ? (Microsoft.Extensions.Primitives.StringValues)"no-cache" : (Microsoft.Extensions.Primitives.StringValues)"public,max-age=31536000,immutable";
	}
});

app.MapControllers();

app.MapHub<ScheduleHub>("/api/hubs/schedule");

app.MapFallbackToFile("index.html");

app.UseCors("CORS");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.Run();