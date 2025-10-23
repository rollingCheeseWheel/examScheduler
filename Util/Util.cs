using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
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

public sealed class RegisterPath(string value) : StringEnum(value)
{
	public static readonly RegisterPath Api = new("v2/api");

	public static readonly RegisterPath LoginPage = new("login");

	public static readonly RegisterPath Login = new("auth/login");
	public static readonly RegisterPath Calendar = new("calendar/student");
	public static readonly RegisterPath ProfileDetails = new("profile/get");

	public override string ToString()
	{
		return base.ToString().EndsWith('/')
			? throw new Exception("Paths cannot end in slashes")
			: base.ToString();
	}
}

public class StringEnum
{
	public readonly string Value;

	protected StringEnum(string value) => Value = value;

	public override string ToString() => Value;

	public static implicit operator string(StringEnum @enum) => @enum.ToString();
	public static bool operator ==(StringEnum? a, StringEnum? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a?.Value == b?.Value;
	}
	public static bool operator !=(StringEnum? left, StringEnum? right) => !( left == right );

	public override bool Equals(object? obj) => obj is StringEnum other && this == other;

	public override int GetHashCode() => Value.GetHashCode();
}