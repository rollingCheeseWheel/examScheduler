using System.Text;
using System.Text.Json;

namespace Util.Extensions;

public static class TextExtensions
{
	public static string ToBase64(this byte[ ] bytes) => Convert.ToBase64String(bytes);

	public static byte[ ] GetBytes(this string str) => Encoding.UTF8.GetBytes(str);

	public static async Task<string> ReadContentAsStringAsync(this HttpResponseMessage message, CancellationToken ct = default) => await message.Content.ReadAsStringAsync(ct);

	public static string ToJson(this object? obj, JsonSerializerOptions options) => JsonSerializer.Serialize(obj, options);

	public static string ToJson(this object? obj) => obj.ToJson(Constants.SerializerOptions);

	public static async Task<string> ToJsonAsync<T>(this Task<T> task, JsonSerializerOptions options, CancellationToken ct = default) => ( await task.WaitAsync(ct) ).ToJson(options);

	public static async Task<string> ToJsonAsync<T>(this Task<T> task, CancellationToken ct = default) => await task.ToJsonAsync(Constants.SerializerOptions, ct);

	public static T? Json<T>(this string str, JsonSerializerOptions options)
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

	public static T? Json<T>(this string str) => str.Json<T>(Constants.SerializerOptions);

	public static async Task<T?> JsonAsync<T>(this Task<string> task, JsonSerializerOptions options, CancellationToken ct = default) => ( await task.WaitAsync(ct) ).Json<T>(options);

	public static async Task<T?> JsonAsync<T>(this Task<string> task, CancellationToken ct = default) => ( await task.WaitAsync(ct) ).Json<T>();

	public static T JsonClone<T>(this T obj) => obj.ToJson().Json<T>()!;

	public static T JsonClone<T>(this T obj, JsonSerializerOptions options) => obj.ToJson(options).Json<T>(options)!;
}
