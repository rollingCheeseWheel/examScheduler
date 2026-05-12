using System.Text.Json;
using System.Text.Json.Serialization;

namespace Util.Converters;

public class EnumConverter<TEnum> : JsonConverter<TEnum>
	where TEnum : struct, Enum
{
	public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.String)
		{
			var value = reader.GetString();
			if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var result))
			{
				return result;
			}
		}

		//if (reader.TokenType == JsonTokenType.Number)
		//{
		//	var intValue = reader.GetInt32();
		//	return (TEnum)Enum.ToObject(typeof(TEnum), intValue);
		//}

		throw new JsonException($"Unable to convert to {typeof(TEnum).Name}");
	}

	public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString().ToLowerInvariant());
}

public class NullableEnumConverter<TEnum> : JsonConverter<TEnum?>
	where TEnum : struct, Enum
{
	public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.String)
		{
			var value = reader.GetString();
			if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var result))
			{
				return result;
			}
		}
		return null;
	}
	public override void Write(Utf8JsonWriter writer, TEnum? value, JsonSerializerOptions options)
	{
		if (value.HasValue)
		{
			writer.WriteStringValue(value.Value.ToString().ToLowerInvariant());
		}
		else
		{
			writer.WriteNullValue();
		}
	}
}