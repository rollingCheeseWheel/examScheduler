using Entities;
using examScheduler;
using examScheduler.Data;
using examScheduler.Services;
using FluffySpoon.AspNet.EncryptWeMust;
using FluffySpoon.AspNet.EncryptWeMust.EntityFramework;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using Util;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("postgres")));

builder.Services.AddIdentity<UserProfile, IdentityRole<int>>()
	.AddEntityFrameworkStores<AppDbContext>()
	.AddDefaultTokenProviders();

// key vault
if (builder.Configuration.GetConnectionString("keyvault") is not null)
{
	builder.AddAzureKeyVaultClient("keyvault");
}

/*////*/
builder.Services
	.AddScoped<ISchoolsService, SchoolsService>()
	.AddScoped<IAuthService, AuthService>()
	.AddScoped<IClassroomService, ClassroomService>()
	.AddSingleton<IKeyVaultService, KeyVaultService>();
/*////*/

// let's encrypt certificate
/*builder.Services.AddFluffySpoonLetsEncrypt(new()
{
	Email = "manuel.sinner0608@wfo-bruneck.info",
	UseStaging = true, // true for testing, false for prod
	Domains = [ "examscheduler.app" ],
	TimeUntilExpiryBeforeRenewal = TimeSpan.FromDays(60),
	TimeAfterIssueDateBeforeRenewal = TimeSpan.FromDays(30),
	RenewalFailMode = FluffySpoon.AspNet.EncryptWeMust.Certes.RenewalFailMode.LogAndContinue
});
builder.Services.AddFluffySpoonLetsEncryptMemoryChallengePersistence();
builder.Services.AddFluffySpoonLetsEncryptEntityFrameworkCertificatePersistence<AppDbContext>(
	// creating the certificate
	async (context, key, bytes) => 
	{
		var existingCertificate = context.Certificates.FirstOrDefault(c => c.Key == (int)key);
		if (existingCertificate is not null)
		{
			existingCertificate.Bytes = bytes;
		}
		else
		{
			context.Certificates.Add(new()
			{
				Key = (int)key,
				Bytes = bytes
			});
		}
	},
	// retrieving the certificate
	async (context, key) => context.Certificates.FirstOrDefault(c => c.Key == (int)key)?.Bytes 
);*/

var tokenValidationParameters = new TokenValidationParameters
{
	ValidateLifetime = true,
	ClockSkew = TimeSpan.FromSeconds(30),

	ValidateIssuerSigningKey = true,
	IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration[ "JWT:key" ]!)),

	ValidateIssuer = true,
	ValidIssuer = builder.Configuration[ "JWT:issuer" ],

	ValidateAudience = true,
	ValidAudience = builder.Configuration[ "JWT:audience" ]
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

	using var scope = app.Services.CreateScope();
	var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	db.Database.Migrate();
}

using (var schoolScope = app.Services.CreateScope())
{
	var db = schoolScope.ServiceProvider.GetRequiredService<AppDbContext>();

	List<School> schools = [
		new School()
		{
			Name = "Test WFO Bruneck Innichen",
			RegisterUri = new("https://wfo-bruneck.digitalesregister.it/"),
			SchoolId = "wfo-bruneck",
			ClientID = "6767"
		},
	];

	if (!db.Schools.SequenceEqual(schools, x => x))
	{
		db.Schools.RemoveRange(db.Schools.ToList());
		db.Schools.AddRange(schools);
	}

	db.SaveChanges();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
