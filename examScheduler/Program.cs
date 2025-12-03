using Entities;
using examScheduler.Data;
using examScheduler.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
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

/*////*/
builder.Services
	.AddScoped<ISchoolsService, SchoolsService>()
	.AddScoped<IAuthService, AuthService>()
	.AddScoped<IClassroomService, ClassroomService>()
	.AddScoped<ITokenProvider, TokenProvider>();
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
	IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration[ "jsonwebtokensigningkey" ]! ?? Guid.NewGuid().ToString("N"))),

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
builder.Services.AddOpenApi();

var app = builder.Build();


app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();

	using var schoolScope = app.Services.CreateScope();
	var db = schoolScope.ServiceProvider.GetRequiredService<AppDbContext>();
	db.Database.Migrate();

	List<Entities.School> schools = [
		new()
		{
			Name = "Test WFO Bruneck Innichen",
			RegisterUri = new("https://wfo-test-bruneck.digitalesregister.it/"),
			SchoolId = "wfo-bruneck",
			ClientId = "QYffPSN5bcsrZ9yL",
			Secret = app.Configuration["QYffPSN5bcsrZ9yL"]!
		},
	];

	if (!db.Schools.SequenceEqual(schools, x => x))
	{
		db.Schools.RemoveRange(db.Schools.ToList());
		db.Schools.AddRange(schools);
	}

	db.SaveChanges();
}

app.MapControllers();

app.UseCors("CORS");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.Run();
