using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Net.Http.Headers;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Util;

public static class Extensions
{
	public const string Format = "yyyy-MM-dd";

	public static string ToRegisterFormat(this DateTimeOffset DateTimeOffset) => DateTimeOffset.ToString(Format);

	public static DateTimeOffset RegisterParse(this string dateTime) => DateTimeOffset.ParseExact(dateTime, Format, null);

	public static bool RegisterTryParse(this string dateTime, [NotNullWhen(true)] out DateTimeOffset? result)
	{
		if (DateTimeOffset.TryParseExact(dateTime, Format, null, DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowTrailingWhite, out var temp))
		{
			result = temp;
			return true;
		}
		result = null;
		return false;
	}

	public static DateTimeOffset RoundToMonday(this DateTimeOffset date)
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

	public static async Task<string> ToJsonAsync<T>(this Task<T?> task, JsonSerializerOptions options, CancellationToken ct = default) => ( await task.WaitAsync(ct) ).ToJson(options);

	public static async Task<string> ToJsonAsync<T>(this Task<T?> task, CancellationToken ct = default) => await task.ToJsonAsync(Constants.SerializerOptions, ct);

	public static IActionResult ServerError(this ControllerBase _) => new StatusCodeResult(500);

	public static Stopwatch Print(this Stopwatch stopwatch)
	{
		Console.WriteLine($"Time elapsed: {stopwatch.ElapsedMilliseconds} ms");
		return stopwatch;
	}

	public static int RoundUpToMultiple(this int value, int multiple) => (int)( multiple * double.Ceiling(value / multiple) );

	public static bool TryValidate(this object value, [NotNullWhen(false)] out ICollection<ValidationResult>? results)
	{
		var validationResults = new List<ValidationResult>();
		var isValid = Validator.TryValidateObject(value, new(value), validationResults, true);
		results = validationResults;
		return isValid;
	}

	public static bool TryValidate(this object value)
	{
		return value.TryValidate(out var _);
	}
}
