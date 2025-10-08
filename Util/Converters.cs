using System.Text.Json;
using System.Text.Json.Serialization;

namespace Util;
public class IntToBoolConverter : JsonConverter<bool>
{
	public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Number)
		{
			int value = reader.GetInt32();
			return value != 0;
		}

		if (reader.TokenType == JsonTokenType.True)
			return true;
		if (reader.TokenType == JsonTokenType.False)
			return false;

		throw new JsonException($"Unexpected token {reader.TokenType} when parsing bool.");
	}

	public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
	{
		writer.WriteNumberValue(value ? 1 : 0);
	}
}

public class RegisterDateConverter : JsonConverter<DateTime>
{
	public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.String)
			throw new JsonException("Expected string for date.");

		string? value = reader.GetString();
		return value!.RegisterParse();
	}

	public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToRegisterFormat());
	}
}