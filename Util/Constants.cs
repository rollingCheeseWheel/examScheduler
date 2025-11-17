using System.Text.Json;

namespace Util;

public static class Constants
{
	public static JsonSerializerOptions SerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true,
		AllowTrailingCommas = true,
	};

	public const string PermissionClaimName = "permissions";
	public const string ClassroomIdClaimName = "classroomId";
	public const string StudentIdClaimName = "studentId";
}
