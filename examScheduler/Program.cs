using Entities;
using examScheduler.BackgroundServices;
using examScheduler.Data;
using examScheduler.Hubs;
using examScheduler.Misc;
using examScheduler.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;
using System.Text;
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
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString(ResourceNames.DBName) + ";Include Error Detail=true"));

builder.Services.AddIdentity<UserProfile, IdentityRole<Guid>>()
	.AddEntityFrameworkStores<AppDbContext>()
	.AddDefaultTokenProviders(); // TODO: shouldnt be needed because of oauth

builder.Services.AddSignalR();

/*// services //*/
builder.Services
	.AddScoped<IAuthService, AuthService>()
	.AddScoped<ICalendarService, CalendarService>()
	.AddScoped<IClassroomService, ClassroomService>()
	.AddScoped<IScheduleService, ScheduleService>()
	.AddScoped<ISchoolsService, SchoolsService>()
	.AddScoped<ITokenProvider, TokenProvider>();
/*////*/

/*// singletons //*/
builder.Services
	.AddSingleton<IDigitalRegisterClientService, DigitalRegisterClientService>();
/*////*/

builder.Services.AddTransient<HttpsEnforcingHandler>();
builder.Services.AddHttpClient("secure")
	.AddHttpMessageHandler<HttpsEnforcingHandler>()
	.AddPolicyHandler(GetRetryPolicy());

/*// background workers //*/
builder.Services
	.AddHostedService<IEventWorker, EventWorker>();
/*////*/

var tokenValidationParameters = new JwtOptions
{
	RefreshTokenBitStrength = 256,
	TokenExpirationInMinutes = 10,
	RefreshTokenExpirationInMinutes = 30,
	MaxTokensPerUser = 3,

	ValidateLifetime = true,
	ClockSkew = TimeSpan.FromSeconds(30),

	ValidateIssuerSigningKey = true,
	IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration[ "JWT:key" ] ?? Guid.CreateVersion7().ToString("N"))),

	ValidateIssuer = true,
	ValidIssuer = "examscheduler.app",

	ValidateAudience = true,
	ValidAudience = "examscheduler.app",
};

builder.Services.AddSingleton(tokenValidationParameters);

builder.Services.AddAuthentication(options =>
{
	options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
	options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
	options.TokenValidationParameters = tokenValidationParameters;

	options.Events = new()
	{
		OnMessageReceived = (ctx) =>
		{
			ctx.Request.Cookies.TryGetValue(IAuthService.AccessTokenCookieName, out var cookie);
			ctx.Token = cookie;
			return Task.CompletedTask;
		}
	};
});

builder.Services.AddAuthorization();

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

//app.UseResponseCompression();

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

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
	return HttpPolicyExtensions
		.HandleTransientHttpError()
		.WaitAndRetryAsync(3, retryAttempt =>
			TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}