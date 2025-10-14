using Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Util;

namespace examScheduler.Identity;

public static class IdentityHelper
{
	public static JwtSecurityToken GetJWT(
		this IServiceProvider serviceProvider,
		int classroomId,
		int studentId,
		Permission permissions,
		int expiresInMinutes = 5,
		int notBeforeMinutes = 1
	)
	{
		var tokenValidationOptions = serviceProvider.GetRequiredService<TokenValidationParameters>();

		var creds = new SigningCredentials(tokenValidationOptions.IssuerSigningKey, SecurityAlgorithms.HmacSha256);

		var claims = new[ ]
		{
			new Claim(Constants.PermissionClaimName, ((int)permissions).ToString()),
			new Claim(Constants.ClassroomIdClaimName, classroomId.ToString()),
			new Claim(Constants.StudentIdClaimName, studentId.ToString())
		};

		return new(
			tokenValidationOptions.ValidIssuer,
			tokenValidationOptions.ValidAudience,
			claims,
			DateTime.UtcNow.AddMinutes(notBeforeMinutes * -1),
			DateTime.UtcNow.AddMinutes(expiresInMinutes),
			creds);

	}

	public static JwtSecurityToken GetJWT(
		this Student student,
		IServiceProvider config,
		int expiresInMinutes = 5,
		int notBeforeMinutes = 1
	)
	{
		return config.GetJWT(
			student.Classroom.Id,
			student.Id,
			student.Permissions,
			expiresInMinutes, notBeforeMinutes
		);
	}

	public static JwtSecurityToken GetJWT(
		this IServiceProvider config,
		Student student,
		int expiresInMinutes = 5,
		int notBeforeMinutes = 1
	)
	{
		return student.GetJWT(config, expiresInMinutes, notBeforeMinutes);
	}

	public static string GetString(this JwtSecurityToken token)
	{
		return new JwtSecurityTokenHandler().WriteToken(token);
	}
}