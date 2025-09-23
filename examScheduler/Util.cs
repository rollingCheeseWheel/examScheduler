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

internal class StringEnum
{
	public readonly string Value;

	public StringEnum(string value) => Value = value;

	public static implicit operator string(StringEnum value) => value.Value;
	public static bool operator ==(StringEnum? left, StringEnum? right) => left?.Value == right?.Value;
	public static bool operator !=(StringEnum? left, StringEnum? right) => !( left == right );
}