using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Util.Converters;

public class RegisterAPIDateTimeConverter() : GenericDateTimeConverter("yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd H:mm:ss");

public class RegisterDateTimeConverter() : GenericDateTimeConverter(Extensions.RegisterDateTimeFormat);

public class GenericDateTimeConverter : JsonConverter<DateTime>
{
	public readonly string[ ] DateTimeFormats = [ "yyyy-MM-ddTHH:mm:ss.fffZ" ];

	public GenericDateTimeConverter() { }
	public GenericDateTimeConverter(params string[ ] dateTimeFormat)
	{
		DateTimeFormats = dateTimeFormat;
	}

	public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is not JsonTokenType.String)
		{
			throw new JsonException($"Can only parse strings as dates, token is of type {reader.TokenType}");
		}

		var value = reader.GetString() ?? throw new JsonException("Token is null");

		foreach (var format in DateTimeFormats)
		{
			try
			{
				return DateTime.ParseExact(value, format, null);
			}
			catch (FormatException) { continue; }
		}
		throw new JsonException($"No valid format specified! Specified formats: {string.Join(", ", DateTimeFormats)}");
	}

	public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToString(DateTimeFormats.First()));
	}
}
