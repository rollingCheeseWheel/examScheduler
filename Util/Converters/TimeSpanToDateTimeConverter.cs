using System.Text.Json;
using System.Text.Json.Serialization;

namespace Util.Converters;

public class TimeSpanToDateTimeConverter : JsonConverter<TimeSpan>
{
	public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.TokenType == JsonTokenType.String &&
			DateTime.TryParse(reader.GetString(), null, System.Globalization.DateTimeStyles.RoundtripKind, out var date)
			? date - DateTime.UnixEpoch
			: throw new JsonException("Token is not of type string or isn't formatted in a ISO DateTime format");

	public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options) => writer.WriteStringValue(( DateTime.UnixEpoch + value ).ToString("O"));
}
