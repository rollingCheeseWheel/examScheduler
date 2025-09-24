namespace examScheduler;

public static class Extensions
{
	public const string Format = "yyyy-MM-dd";

	public static string RegisterFormat(this DateTime dateTime) => dateTime.ToString(Format);

	public static DateTime RegisterParse(this string dateTime) => DateTime.ParseExact(dateTime, Format, null);

	public static Uri GetSchemeAndAuthority(this Uri uri) => new(uri.Scheme + Uri.SchemeDelimiter + uri.Authority);

}
