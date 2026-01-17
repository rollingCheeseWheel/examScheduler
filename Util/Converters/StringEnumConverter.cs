using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Util.Converters;

public class StringEnumConverter<T> : JsonConverter<T> where T : StringEnum
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (value is null) return null;

        var fields = typeof(T)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(T));

        foreach (var f in fields)
        {
            var obj = f.GetValue(null);
            if (obj is null) continue;
            var inst = (T)obj;
            if (inst.Value == value)
                return inst;
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        => writer.WriteStringValue(value?.Value);
}
