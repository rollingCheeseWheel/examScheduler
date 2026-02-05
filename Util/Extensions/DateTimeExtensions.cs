using System.Globalization;

namespace Util.Extensions;

public static class DateTimeExtensions
{
	public const string RegisterDateTimeFormat = "yyyy-MM-dd";

	public static string ToRegisterFormat(this DateTimeOffset DateTimeOffset) => DateTimeOffset.ToString(RegisterDateTimeFormat);

	public static DateTimeOffset RegisterParse(this string dateTime) => DateTimeOffset.ParseExact(dateTime, RegisterDateTimeFormat, null);

	public static bool RegisterTryParse(this string dateTime, out DateTimeOffset result)
	{
		if (DateTimeOffset.TryParseExact(dateTime, RegisterDateTimeFormat, null, DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowTrailingWhite, out var temp))
		{
			result = temp;
			return true;
		}
		result = default;
		return false;
	}

	public static DateTimeOffset RoundDownToMonday(this DateTimeOffset date)
	{
		var diff = ( date.DayOfWeek - DayOfWeek.Monday + 7 ) % 7;
		return date.AddDays(-diff).Date;
	}

	public static DateTimeOffset RoundUpTo(this DateTimeOffset date, DayOfWeek dayOfWeek)
	{
		int daysToAdd = ( (int)dayOfWeek - (int)date.DayOfWeek + 7 ) % 7;
		if (daysToAdd == 0) daysToAdd = 7; // always round *up* to the next occurrence
		return date.AddDays(daysToAdd);
	}
}
