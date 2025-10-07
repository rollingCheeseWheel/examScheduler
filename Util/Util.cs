using System.Security.Cryptography;
using System.Text.Json;

namespace Util;

public static class Util
{
	public static byte[ ] GenerateRandomSalt(int length = 32)
	{
		var salt = new byte[ length ];
		RandomNumberGenerator.Fill(salt);
		return salt;
	}

	public static string GenerateSaltBase64(int length = 32) => GenerateRandomSalt(length).ToBase64();
}

public static class Constants
{
	public static JsonSerializerOptions SerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true,
		AllowTrailingCommas = true,
	};
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

	public static implicit operator string(StringEnum @enum) => @enum.ToString();
	public static bool operator ==(StringEnum? left, StringEnum? right) => left?.Value == right?.Value;
	public static bool operator !=(StringEnum? left, StringEnum? right) => !( left == right );

	public override string ToString() => Value;

	public override bool Equals(object? obj)
	{
		if (ReferenceEquals(this, obj))
		{
			return true;
		}
		else if (obj is StringEnum asEnum)
		{
			return this == asEnum;
		}
		return false;
	}

	public override int GetHashCode() => Value.GetHashCode();
}