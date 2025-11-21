using System.Text.Json;
using System.Text.Json.Serialization;

namespace Util.Converters;

public class RegisterAPIDateTimeConverter() : GenericDateTimeConverter("yyyy-MM-dd HH:mm:ss");

public class RegisterDateTimeConverter() : GenericDateTimeConverter(Extensions.RegisterDateTimeFormat);

public class GenericDateTimeConverter : JsonConverter<DateTime>
{
	public readonly string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

	public GenericDateTimeConverter() { }
	public GenericDateTimeConverter(string dateTimeFormat)
	{
		DateTimeFormat = dateTimeFormat;
	}

	public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is JsonTokenType.String)
		{
			return DateTime.ParseExact(reader.GetString() ?? throw new JsonException("Token is null"), DateTimeFormat, null);
		}
		else
		{
			throw new JsonException("Token is not of type string");
		}
	}

	public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToString(DateTimeFormat));
	}
}
