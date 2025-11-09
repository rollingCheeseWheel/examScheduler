using System.Text.Json;
using System.Text.Json.Serialization;

namespace Util.Converters;
public class DateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
	private const string DateTimeFormat = "yyyy-MM-dd'T'HH:mm:ss.FFFFFF'Z'";

	public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.String)
			throw new JsonException("Expected string for DateTimeOffset.");

		var raw = reader.GetString();
		if (string.IsNullOrWhiteSpace(raw))
			throw new JsonException("Empty date string.");

		// Try direct parse first
		if (DateTimeOffset.TryParse(raw, out var dto))
		{
			// Enforce UTC: convert any offset to UTC (option A)
			dto = dto.ToUniversalTime();

			// If instead you want to reject non-UTC instead of converting, uncomment:
			// if (dto.Offset != TimeSpan.Zero)
			//     throw new JsonException($"Non-UTC offset '{dto.Offset}' is not allowed.");

			return dto;
		}

		// Fallback: try parse as DateTime (naive becomes UTC)
		if (DateTime.TryParse(raw, out var dt))
		{
			var unspecifiedOrLocal = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
			return new DateTimeOffset(unspecifiedOrLocal, TimeSpan.Zero);
		}

		throw new JsonException($"Could not parse DateTimeOffset value '{raw}'.");
	}

	public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
	{
		// Normalize to UTC
		var utc = value.ToUniversalTime();
		writer.WriteStringValue(utc.ToString(DateTimeFormat));
	}
}
