using System.Text;
using System.Text.Json;

namespace Util.Extensions;

public static class TextExtensions
{
	public static string ToBase64(this byte[ ] bytes) => Convert.ToBase64String(bytes);

	public static byte[ ] GetBytes(this string str) => Encoding.UTF8.GetBytes(str);

	public static async Task<string> ReadContentAsStringAsync(this HttpResponseMessage message, CancellationToken ct = default) => await message.Content.ReadAsStringAsync(ct);

	public static string Stringify(this object? obj, JsonSerializerOptions options) => JsonSerializer.Serialize(obj, options);

	public static string Stringify(this object? obj) => obj.Stringify(Constants.SerializerOptions);

	public static async Task<string> StringifyAsync<T>(this Task<T> task, JsonSerializerOptions options, CancellationToken ct = default) => ( await task.WaitAsync(ct) ).Stringify(options);

	public static async Task<string> StringifyAsync<T>(this Task<T> task, CancellationToken ct = default) => await task.StringifyAsync(Constants.SerializerOptions, ct);

	public static T? JsonParse<T>(this string str, JsonSerializerOptions options)
	{
		try
		{
			return JsonSerializer.Deserialize<T>(str, options);
		}
		catch
		{
			return default;
		}
	}

	public static T? JsonParse<T>(this string str) => str.JsonParse<T>(Constants.SerializerOptions);

	public static async Task<T?> JsonParseAsync<T>(this Task<string> task, JsonSerializerOptions options, CancellationToken ct = default) => ( await task.WaitAsync(ct) ).JsonParse<T>(options);

	public static async Task<T?> JsonParseAsync<T>(this Task<string> task, CancellationToken ct = default) => ( await task.WaitAsync(ct) ).JsonParse<T>();

	public static T JsonClone<T>(this T obj) => obj.Stringify().JsonParse<T>()!;

	public static T JsonClone<T>(this T obj, JsonSerializerOptions options) => obj.Stringify(options).JsonParse<T>(options)!;
}
