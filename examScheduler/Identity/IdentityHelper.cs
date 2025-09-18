using examScheduler.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace examScheduler.Identity;

public static class IdentityHelper
{
	public const string PermissionClaimName = "permissions";
	public const string ClassroomIdClaimName = "classroomId";
	public const string StudentIdClaimName = "studentId";

	public static JwtSecurityToken GetJWT(
		this IConfiguration config,
		int classroomId,
		int studentId,
		Permission permissions,
		int expiresInMinutes = 5,
		int notBeforeMinutes = 1
	)
	{
		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config[ "JWT:key" ]!));
		var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		var claims = new[ ]
		{
			new Claim(PermissionClaimName, ((int)permissions).ToString()),
			new Claim(ClassroomIdClaimName, classroomId.ToString()),
			new Claim(StudentIdClaimName, studentId.ToString())
		};

		return new(
			config[ "JWT:issuer" ],
			config[ "JWT:audience" ],
			claims,
			DateTime.UtcNow.AddMinutes(notBeforeMinutes * -1),
			DateTime.UtcNow.AddMinutes(expiresInMinutes),
			creds);

	}

	public static JwtSecurityToken GetJWT(
		this Student student,
		IConfiguration config,
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
		this IConfiguration config,
		Student student,
		int expiresInMinutes = 5,
		int notBeforeMinutes = 1
	)
	{
		return student.GetJWT(config, expiresInMinutes, notBeforeMinutes);
	}

	public static string GetJWTString(this JwtSecurityToken token)
	{
		return new JwtSecurityTokenHandler().WriteToken(token);
	}
}