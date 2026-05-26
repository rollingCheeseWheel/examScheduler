using System.Globalization;

namespace Util.Extensions;

public static class DateTimeExtensions
{
	public const string RegisterDateTimeFormat = "yyyy-MM-dd";

	public static string ToRegisterFormat(this DateTimeOffset DateTimeOffset) => DateTimeOffset.ToString(RegisterDateTimeFormat);

	public static DateTimeOffset RegisterParse(this string dateTime) => DateTimeOffset.ParseExact(dateTime, RegisterDateTimeFormat, null);

	public static bool TryParseRegisterDate(this string dateTime, out DateTimeOffset result)
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

	public static DateOnly RoundDownToMonday(this DateOnly date) => date.ToDateTimeOffset().RoundDownToMonday().ToDateOnly();

	public static DateTimeOffset RoundUpTo(this DateTimeOffset date, DayOfWeek dayOfWeek, bool roundToNextWeekIfSameDay = true)
	{
		var daysToAdd = ( (int)dayOfWeek - (int)date.DayOfWeek + 7 ) % 7;
		if (daysToAdd is 0 && roundToNextWeekIfSameDay)
		{
			daysToAdd = 7; // always round *up* to the next occurrence
		}

		return date.AddDays(daysToAdd);
	}

	public static DateOnly RoundUpTo(this DateOnly date, DayOfWeek dayOfWeek)
	{
		var daysToAdd = ( (int)dayOfWeek - (int)date.DayOfWeek + 7 ) % 7;
		if (daysToAdd is 0)
		{
			daysToAdd = 7;
		}
		return date.AddDays(daysToAdd);
	}

	public static long GetWeek(this DateTimeOffset date) => (long)( ( date.ToUniversalTime() - DateTimeOffset.MinValue ).TotalDays / 7 );

	public static long GetWeek(this DateTime date) => (long)( ( date.ToUniversalTime() - DateTime.MinValue ).TotalDays / 7 );

	public static long GetWeek(this DateOnly date) => date.ToDateTime().GetWeek();

	public static DateOnly ToDateOnly(this DateTimeOffset date) => DateOnly.FromDateTime(date.DateTime);

	public static DateOnly ToDateOnly(this DateTime date) => DateOnly.FromDateTime(date);

	public static DateTimeOffset ToDateTimeOffset(this DateOnly date) => new(date.ToDateTime(), TimeSpan.Zero);

	public static DateTime ToDateTime(this DateOnly date) => date.ToDateTime(TimeOnly.MinValue);
}
