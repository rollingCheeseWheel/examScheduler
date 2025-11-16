using Entities;
using examScheduler;
using examScheduler.Data;
using examScheduler.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Writers;
using System.Text;
using System.Text.Json;
using Util;
using Util.Converters;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddKeyVaultCache(builder.Configuration.GetConnectionString("keyvault"));

// Add services to the container.

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("postgres")));

builder.Services.AddIdentity<UserProfile, IdentityRole<int>>()
	.AddEntityFrameworkStores<AppDbContext>()
	.AddDefaultTokenProviders();

/*////*/
builder.Services
	.AddScoped<ISchoolsService, SchoolsService>()
	.AddScoped<IAuthService, AuthService>()
	.AddScoped<IClassroomService, ClassroomService>();
/*////*/

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
