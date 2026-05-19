namespace Util;

public static class DateUtils
{
	public static DateTimeOffset Min(DateTimeOffset value, DateTimeOffset min) => value > min ? value : min;
	public static DateTime Min(DateTime value, DateTime min) => value > min ? value : min;

	public static DateTimeOffset Max(DateTimeOffset value, DateTimeOffset max) => Min(max, value);
	public static DateTime Max(DateTime value, DateTime max) => Min(max, value);
}
