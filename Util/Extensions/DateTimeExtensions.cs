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
		var diff = ( 7 + ( date.DayOfWeek - DayOfWeek.Monday ) ) % 7;
		return date.AddDays(-diff).Date;
	}
}
