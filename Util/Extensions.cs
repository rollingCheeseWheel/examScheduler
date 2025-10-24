using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Util;

public static class Extensions
{
	public const string Format = "yyyy-MM-dd";

	public static string ToRegisterFormat(this DateTime dateTime) => dateTime.ToString(Format);

	public static DateTime RegisterParse(this string dateTime) => DateTime.ParseExact(dateTime, Format, null);

	public static bool RegisterTryParse(this string dateTime, [NotNullWhen(true)] out DateTime? result)
	{
		if (DateTime.TryParseExact(dateTime, Format, null, DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowTrailingWhite, out var temp))
		{
			result = temp;
			return true;
		}
		result = null;
		return false;
	}

	public static DateTime RoundToMonday(this DateTime date)
	{
		int diff = ( 7 + ( date.DayOfWeek - DayOfWeek.Monday ) ) % 7;
		return date.AddDays(-diff).Date;
	}

	public static Uri GetSchemeAndAuthority(this Uri uri) => new(uri.Scheme + Uri.SchemeDelimiter + uri.Authority);

	public static Uri AppendRelativePath(this Uri uri, string relativePath)
	{
		var output = new Uri(uri.ToString() + ( relativePath.StartsWith('/') || uri.ToString().EndsWith('/') ? "" : "/" ) + relativePath);
		return output;
	}

	public static Uri GetBaseApiPath(this Uri uri) => uri.GetSchemeAndAuthority().AppendRelativePath(RegisterPath.Api);

	public static string ToBase64(this byte[ ] bytes) => Convert.ToBase64String(bytes);

	public static byte[ ] GetBytes(this string str) => Encoding.UTF8.GetBytes(str);

	public static async Task<string> ReadContentAsStringAsync(this HttpResponseMessage message, CancellationToken ct = default)
	{
		return await message.Content.ReadAsStringAsync(ct);
	}

	public static string ToJson(this object? obj, JsonSerializerOptions options) => JsonSerializer.Serialize(obj, options);

	public static string ToJson(this object? obj) => obj.ToJson(Constants.SerializerOptions);

	public static async Task<string> ToJsonAsync<T>(this Task<T?> task, JsonSerializerOptions options) => (await task).ToJson(options);

	public static async Task<string> ToJsonAsync<T>(this Task<T?> task) => await task.ToJsonAsync(Constants.SerializerOptions);

	public static IActionResult ServerError(this ControllerBase _) => new StatusCodeResult(500);

	public static Stopwatch Print(this Stopwatch stopwatch){
		Console.WriteLine($"Time elapsed: {stopwatch.ElapsedMilliseconds} ms");
		return stopwatch;
	}
}
