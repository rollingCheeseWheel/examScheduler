using ExamScheduler.Entities;
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

	public static JwtSecurityToken GetJWT(this Student student, IConfiguration config, int expiresInMinutes = 5)
	{
		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config[ "JWT:key" ]!));
		var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		var claims = new[ ]
		{
			new Claim(PermissionClaimName, ((int)student.Permissions).ToString()),
			new Claim(ClassroomIdClaimName, student.Classroom.Id.ToString()),
			new Claim(StudentIdClaimName, student.Id.ToString())
		};

		return new(
			config[ "JWT:issuer" ],
			config[ "JWT:audience" ],
			claims,
			DateTime.UtcNow.AddMinutes(-1),
			DateTime.UtcNow.AddMinutes(expiresInMinutes), creds);
	}

	public static JwtSecurityToken GetJWT(this IConfiguration config, Student student)
	{
		return student.GetJWT(config);
	}

	public static string GetJWTString(this JwtSecurityToken token)
	{
		return new JwtSecurityTokenHandler().WriteToken(token);
	}
}