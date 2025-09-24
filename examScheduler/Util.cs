using System.Numerics;
using System.Text.Json;

namespace examScheduler;

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
	public readonly RegisterPath Api = new("v2/api/");

	public readonly RegisterPath AuthLogin = new("auth/login/");
	public readonly RegisterPath Calendar = new("calendar/student/");
}

public class StringEnum
{
	public readonly string Value;

	protected StringEnum(string value) => Value = value;

	public static implicit operator string(StringEnum value) => value.Value;
	public static bool operator ==(StringEnum? left, StringEnum? right) => left?.Value == right?.Value;
	public static bool operator !=(StringEnum? left, StringEnum? right) => !( left == right );

	public override string ToString() => this;

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