using System.Text.Json;
using System.Text.Json.Serialization;

namespace Util.Converters;

public class IntToBoolConverter : JsonConverter<bool>
{
	public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.TokenType switch
	{
		JsonTokenType.Number => reader.GetInt32() != 0,
		JsonTokenType.True => true,
		JsonTokenType.False => false,
		JsonTokenType.Null => false,
		_ => throw new JsonException($"Unexpected token {reader.TokenType} when parsing bool.")
	};

	public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) => writer.WriteBooleanValue(value);
}
