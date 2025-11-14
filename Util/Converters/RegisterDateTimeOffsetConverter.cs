using System.Text.Json;
using System.Text.Json.Serialization;

namespace Util.Converters;

public class RegisterDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
	public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.String)
			throw new JsonException("Expected string for date.");

		var value = reader.GetString();
		return value!.RegisterParse();
	}

	public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToRegisterFormat());
	}
}