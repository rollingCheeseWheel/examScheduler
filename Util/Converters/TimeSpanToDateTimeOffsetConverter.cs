using System.Text.Json;
using System.Text.Json.Serialization;

namespace Util.Converters;

public class TimeSpanToDateTimeOffsetConverter : JsonConverter<TimeSpan>
{
	public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is not JsonTokenType.String)
		{
			throw new JsonException("Token is not a string representation of a date.");
		}

		var value = reader.GetString();
		return string.IsNullOrEmpty(value) || !DateTimeOffset.TryParse(value, out var dto)
			? throw new JsonException("Unable to parse DateTimeOffset from string.")
			: dto - DateTimeOffset.UnixEpoch;
	}

	public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options) => writer.WriteStringValue(( DateTimeOffset.UnixEpoch + value ).ToString("O"));
}
