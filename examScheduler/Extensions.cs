using System.Globalization;

namespace examScheduler;

public static class Extensions
{
	public const string Format = "yyyy-MM-dd";

	public static string RegisterFormat(this DateTime dateTime) => dateTime.ToString(Format);

	public static DateTime RegisterParse(this string dateTime) => DateTime.ParseExact(dateTime, Format, null);

	public static bool RegisterTryParse(this string dateTime, out DateTime? result)
	{
		if (DateTime.TryParseExact(dateTime, Format, null, DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowTrailingWhite, out var temp))
		{
			result = temp;
			return true;
		}
		result = null;
		return false;
	}

	public static Uri GetSchemeAndAuthority(this Uri uri) => new(uri.Scheme + Uri.SchemeDelimiter + uri.Authority);

	public static Uri AppendRelativePath(this Uri uri, string relativePath)
	{
		var output = new Uri(uri.ToString() + ( relativePath.StartsWith('/') || uri.ToString().EndsWith('/') ? "" : "/" ) + relativePath);
		return output;
	}

	public static Uri GetBaseApiPath(this Uri uri) => uri.GetSchemeAndAuthority().AppendRelativePath(RegisterPath.Api);
}
