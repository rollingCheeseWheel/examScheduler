using Entities;
using examScheduler;
using examScheduler.Data;
using examScheduler.Hubs;
using examScheduler.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using Util;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddCors(options =>
{
	options.AddPolicy("CORS", p =>
		p.AllowAnyOrigin()
		.AllowAnyHeader()
		.AllowAnyMethod()
	);
});

var keyvaultConnectionString = builder.Configuration.GetConnectionString(ResourceNames.KeyVault);
if (keyvaultConnectionString is not null)
{
	builder.Configuration.AddAzureKeyVaultSecrets(keyvaultConnectionString);
}

// Add services
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString(ResourceNames.DBName)));

builder.Services.AddIdentity<UserProfile, IdentityRole<Guid>>()
	.AddEntityFrameworkStores<AppDbContext>()
	.AddDefaultTokenProviders(); // TODO: shouldnt be needed because of oauth

builder.Services.AddSignalR();

/*////*/
builder.Services
	.AddScoped<ISchoolsService, SchoolsService>()
	.AddScoped<IAuthService, AuthService>()
	.AddScoped<IClassroomService, ClassroomService>()
	.AddScoped<ITokenProvider, TokenProvider>()
	.AddScoped<IScheduleService, ScheduleService>();
/*////*/

/*////*/
builder.Services
	.AddSingleton<CalendarWorker>()
	.AddSingleton<ICalendarWorker>(sp => sp.GetRequiredService<CalendarWorker>())
	.AddHostedService(sp => sp.GetRequiredService<CalendarWorker>());
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
	IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration[ "JWT:key" ] ?? Guid.NewGuid().ToString("N"))),

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

builder.Services.AddResponseCompression(options =>
{
	options.EnableForHttps = true;
	options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
		[ /*"application/json",*/ "application/javascript", "style/css", "text/html" ]
	);
});

var app = builder.Build();

app.UseResponseCompression();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	/*app.MapOpenApi("{documentName}");*/

	using var schoolScope = app.Services.CreateScope();
	var db = schoolScope.ServiceProvider.GetRequiredService<AppDbContext>();
	db.Database.Migrate();

	List<Entities.School> schools = [
		new()
		{
			Name = "Test WFO Bruneck Innichen",
			RegisterUri = new("https://wfo-test-bruneck.digitalesregister.it/"),
			SchoolId = "wfo-test-bruneck",
			ClientId = "QYffPSN5bcsrZ9yL",
			Secret = app.Configuration["QYffPSN5bcsrZ9yL"]!,
			IsEnabled = true
		},
		new() {
			Name = "some school",
			RegisterUri = new("https://some-school.digitalesregister.it/"),
			SchoolId = "some-school",
			ClientId = "asdfölijasdlfkjhask",
			Secret = "alsdkhjfgxcvhyölhdfjlhasgu",
			IsEnabled = false,
		},
		new() {
			Name = "some other school",
			RegisterUri = new("https://some-other-school.digitalesregister.it/"),
			SchoolId = "some-other-school",
			ClientId = "asdfölijasdlfkjhask",
			Secret = "alsdkhjfgxcvhyölhdfjlhasgu",
			IsEnabled = true,
		}
	];

	if (!db.Schools.Equals(schools, x => x))
	{
		db.Schools.RemoveRange(db.Schools.ToList());
		db.Schools.AddRange(schools);
	}

	db.SaveChanges();
}

app.UseStaticFiles(new StaticFileOptions()
{
	OnPrepareResponse = ctx =>
	{
		var path = ctx.File.PhysicalPath;
		if (path is not null && path.EndsWith("html"))
		{
			ctx.Context.Response.Headers.CacheControl = "no-cache";
		}
		else
		{
			ctx.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
		}
	}
});

app.MapControllers();

app.MapHub<ScheduleHub>("/api/schedule");

app.MapFallbackToFile("index.html");

app.UseCors("CORS");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.Run();
