using Entities;
using examScheduler.Data;
using examScheduler.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("postgres")));

builder.Services.AddIdentity<UserProfile, IdentityRole<int>>()
	.AddEntityFrameworkStores<AppDbContext>();

/*////*/
builder.Services
	.AddScoped<ISchoolService, SchoolService>()
	.AddScoped<IAuthService, AuthService>();
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

	using (var scope = app.Services.CreateScope())
	{
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		db.Database.Migrate();
	}
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
