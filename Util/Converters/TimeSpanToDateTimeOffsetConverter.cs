using Newtonsoft.Json;

namespace Util.Converters;
public class TimeSpanToDateTimeOffsetConverter : JsonConverter<TimeSpan>
{
	public override TimeSpan ReadJson(JsonReader reader, Type objectType, TimeSpan existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		if (reader.TokenType is not JsonToken.Date || reader.TokenType is not JsonToken.String)
		{
			throw new JsonException("Token is not a Date string or Date");
		}

		var dto = reader.TokenType == JsonToken.Date
			? (DateTimeOffset)reader.Value!
			: DateTimeOffset.Parse((string)reader.Value!);
		return dto - DateTimeOffset.UnixEpoch;
	}
	public override void WriteJson(JsonWriter writer, TimeSpan value, JsonSerializer serializer)
	{
		writer.WriteValue(( DateTimeOffset.UnixEpoch + value ).ToString("O"));
	}
}
